using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DownloadServer : IBackgroundService, IDisposable
    {
        readonly AppSettings.HttpSettings _settings;
        readonly HttpListener _http;
        readonly ILogger _logger;

        public DownloadServer(
            IOptions<AppSettings> options,
            ILogger<DownloadServer> logger
        )
        {
            _settings = options.Value.Http;
            _http = new HttpListener();
            _logger = logger;

            var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;
            var prefix = $"http://+:{port}/";
            _http.Prefixes.Add(prefix);

            _logger.LogInformation($"HTTP listening at '{prefix}'.");
        }

        public async Task RunAsync(CancellationToken token)
        {
            _logger.LogInformation($"Starting HTTP server.");

            _http.Start();

            try
            {
                var handlerTasks = new HashSet<Task>();

                while (!token.IsCancellationRequested)
                {
                    // Add new handlers
                    while (handlerTasks.Count < _settings.Concurrency)
                        handlerTasks.Add(HandleRequestAsync());

                    // Remove completed handlers
                    handlerTasks.Remove(await Task.WhenAny(handlerTasks));
                }

                // Wait for all handlers to finish
                await Task.WhenAll(handlerTasks);
            }
            finally { _http.Stop(); }
        }

        public async Task HandleRequestAsync()
        {
            // Listen for request
            var context = await _http.GetContextAsync();

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    await HandleGetAsync(context.Request, context.Response);
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
            }

            context.Response.Close();
        }

        public async Task HandleGetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (request.Url.AbsolutePath)
            {
                case "/":
                    const string index_txt = @"
nhitomi - Discord doujinshi bot by phosphene47#7788

- Discord: https://discord.gg/bf3q7RM
- Github: https://github.com/phosphene47/nhitomi";
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        await writer.WriteAsync(index_txt.Trim());
                        await writer.FlushAsync();
                    }
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
            }
        }

        public void Dispose() => _http.Close();
    }
}