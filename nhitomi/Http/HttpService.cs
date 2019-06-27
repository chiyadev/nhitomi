using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Http
{
    public class HttpService : BackgroundService
    {
        readonly HttpListener _listener;

        readonly AppSettings _settings;
        readonly ProxyHandler _proxyHandler;
        readonly ILogger<HttpService> _logger;

        public HttpService(IOptions<AppSettings> options,
                           ProxyHandler proxyHandler,
                           ILogger<HttpService> logger)
        {
            // use PORT envvar or 80
            var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 80;

            // start listener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");

            _settings     = options.Value;
            _proxyHandler = proxyHandler;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();

                        // handle request asynchronously
                        _ = Task.Run(() => HandleRequestAsync(context, stoppingToken), stoppingToken);
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

        async Task HandleRequestAsync(HttpListenerContext context,
                                      CancellationToken cancellationToken = default)
        {
            var request  = context.Request;
            var response = context.Response;

            try
            {
                switch (request.Url.AbsolutePath)
                {
                    // default endpoint
                    case "/":
                        response.Redirect("https://chiya.dev");
                        return;

                    // proxy endpoint
                    case "/proxy" when _settings.Http.EnableProxy:
                        await _proxyHandler.HandleRequestAsync(context, cancellationToken);
                        return;
                }

                // not found
                response.StatusCode  = 400;
                response.ContentType = "text/plain";

                using (var writer = new StreamWriter(response.OutputStream))
                    await writer.WriteLineAsync("404 Not Found");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception while handling request from {0}.", request.UserHostAddress);
            }
            finally
            {
                // always close response
                response.Close();
            }
        }
    }
}