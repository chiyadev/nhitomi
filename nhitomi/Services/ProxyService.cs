using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace nhitomi.Services
{
    public class ProxyService : IDisposable
    {
        readonly HttpListener _listener;
        readonly HttpClient _client;

        public ProxyService(int port)
        {
            // create client
            _client = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            // start listener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _listener.Start();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();

                        // handle context asynchronously
                        _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception while receiving request: {e}");
                    }
                }
            }
            finally
            {
                _listener.Close();
            }
        }

        async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken = default)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                if (!Uri.TryCreate(
                    HttpUtility.UrlDecode(request.Url.PathAndQuery.TrimStart('/')),
                    UriKind.Absolute,
                    out var forwardUri))
                {
                    response.StatusCode = 400;
                    response.StatusDescription = "Invalid upstream URL.";
                    return;
                }

                Console.WriteLine($"{request.HttpMethod} {forwardUri}");

                // create forward request
                using (var forwardRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.HttpMethod),
                    RequestUri = forwardUri,
                    Version = request.ProtocolVersion,
                    Content = new StreamContent(request.InputStream)
                })
                {
                    // copy headers
                    foreach (string key in request.Headers)
                    {
                        var value = request.Headers[key];

                        Console.WriteLine($"{key}: {value}");

                        switch (key.ToLowerInvariant())
                        {
                            // ignored headers
                            case "host":
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

                    Console.WriteLine("Sending request...");

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
                            switch (key.ToLowerInvariant())
                            {
                                // copy
                                default:
                                    Console.WriteLine($"{key}: {value}");
                                    response.AddHeader(key, value);
                                    break;
                            }
                        }

                        Console.WriteLine("Sending response...");

                        // copy body
                        using (var stream = await forwardResponse.Content.ReadAsStreamAsync())
                            await stream.CopyToAsync(response.OutputStream, cancellationToken);

                        await response.OutputStream.FlushAsync(cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while handling request: {e}");

                response.StatusCode = 500;
            }
            finally
            {
                // always close response
                response.Close();

                Console.WriteLine("Closed response.");
            }
        }

        public void Dispose() => _client.Dispose();
    }
}