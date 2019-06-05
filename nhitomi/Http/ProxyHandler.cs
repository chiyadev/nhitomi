using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Http
{
    public class ProxyHandler : IDisposable
    {
        readonly HttpClient _client;
        readonly ILogger<ProxyHandler> _logger;

        public ProxyHandler(ILogger<ProxyHandler> logger)
        {
            // create http client
            // we don't use DI injected client because we use custom SocketsHttpHandler
            _client = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            _logger = logger;
        }

        public async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken = default)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // validate request url
                var requestUrl = request.Headers.Get("Upstream");

                if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var requestUri))
                {
                    response.StatusCode = 400;
                    response.StatusDescription = "Invalid upstream URL.";
                    return;
                }

                _logger.LogDebug($"{request.HttpMethod} {requestUri}");

                // create forward request
                using (var forwardRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.HttpMethod),
                    RequestUri = requestUri,
                    Version = request.ProtocolVersion,
                    Content = new StreamContent(request.InputStream)
                })
                {
                    // copy headers
                    foreach (string key in request.Headers)
                    {
                        var value = request.Headers[key];

                        _logger.LogDebug($"{key}: {value}");

                        switch (key.ToLowerInvariant())
                        {
                            // ignored
                            case "host":
                            case "upstream":
                                continue;

                            // content headers
                            case "content-length":
                                forwardRequest.Content.Headers.ContentLength = long.Parse(value);
                                break;
                            case "content-type":
                                forwardRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
                                break;

                            // copy
                            default:
                                forwardRequest.Headers.Add(key, value);
                                break;
                        }
                    }

                    _logger.LogDebug("Sending request...");

                    // send request
                    using (var forwardResponse = await _client.SendAsync(forwardRequest, cancellationToken))
                    {
                        response.ProtocolVersion = forwardResponse.Version;
                        response.StatusCode = (int) forwardResponse.StatusCode;
                        response.StatusDescription = forwardResponse.ReasonPhrase;

                        // copy headers
                        foreach (var (key, values) in forwardResponse.Headers)
                        foreach (var value in values)
                        {
                            _logger.LogDebug($"{key}: {value}");

                            switch (key.ToLowerInvariant())
                            {
                                // ignore
                                case "connection":
                                    continue;

                                // copy
                                default:
                                    response.AddHeader(key, value);
                                    break;
                            }
                        }

                        foreach (var (key, values) in forwardResponse.Content.Headers)
                        foreach (var value in values)
                        {
                            _logger.LogDebug($"{key}: {value}");

                            switch (key.ToLowerInvariant())
                            {
                                // copy
                                default:
                                    response.AddHeader(key, value);
                                    break;
                            }
                        }

                        _logger.LogDebug("Sending response...");

                        // copy body
                        using (var stream = await forwardResponse.Content.ReadAsStreamAsync())
                            await stream.CopyToAsync(response.OutputStream, cancellationToken);

                        // flush output
                        await response.OutputStream.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Exception while handling request: {e}");

                response.StatusCode = 500;
            }
        }

        public void Dispose() => _client.Dispose();
    }
}