// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class DownloadServer : IBackgroundService, IDisposable
    {
        readonly AppSettings _settings;
        readonly HttpClient _httpClient;
        readonly ISet<IDoujinClient> _clients;
        readonly ILogger _logger;

        public HttpListener HttpListener { get; }

        public DownloadServer(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            ISet<IDoujinClient> clients,
            ILogger<DownloadServer> logger
        )
        {
            _settings = options.Value;
            _httpClient = httpFactory.CreateClient(nameof(DownloadServer));
            _clients = clients;
            _logger = logger;

            HttpListener = new HttpListener();

            var prefix = $"http://+:{_settings.Http.Port}/";
            HttpListener.Prefixes.Add(prefix);

            _logger.LogDebug($"HTTP listening at '{prefix}'.");
        }

        public async Task RunAsync(CancellationToken token)
        {
            _logger.LogDebug($"Starting HTTP server.");

            HttpListener.Start();

            try
            {
                var handlerTasks = new HashSet<Task>();

                while (!token.IsCancellationRequested)
                {
                    // Add new handlers
                    while (handlerTasks.Count < _settings.Http.Concurrency)
                        handlerTasks.Add(HandleRequestAsync());

                    // Remove completed handlers
                    handlerTasks.Remove(await Task.WhenAny(handlerTasks));
                }

                // Wait for all handlers to finish
                await Task.WhenAll(handlerTasks);
            }
            finally { HttpListener.Stop(); }
        }

        public async Task HandleRequestAsync()
        {
            // Listen for request
            var context = await HttpListener.GetContextAsync();

            try
            {
                switch (context.Request.HttpMethod)
                {
                    case "GET":
                        await HandleGetAsync(context.Request, context.Response);
                        break;

                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while handling HTTP request: {e.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            context.Response.Close();
        }

        static Regex _dlRegex = new Regex(@"^\/dl\/(?<token>.+\..+)$", RegexOptions.Compiled);

        public async Task HandleGetAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var path = request.Url.AbsolutePath;

            if (path == "/")
            {
                const string index_txt = @"
nhitomi - Discord doujinshi bot by phosphene47#7788

- Discord: https://discord.gg/JFNga7q
- Github: https://github.com/phosphene47/nhitomi";
                using (var writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync(index_txt.Trim());
                    await writer.FlushAsync();
                }

                return;
            }
            if (path.StartsWith("/dl/"))
            {
                var match = _dlRegex.Match(path);
                var token = match.Groups.FirstOrDefault(g => g.Success && g.Name == "token")?.Value;

                _logger.LogDebug($"Received download request: token {token}");

                // Parse token
                if (token != null &&
                    TokenGenerator.TryDeserializeToken(
                        token: token,
                        secret: _settings.Discord.Token,
                        sourceName: out var sourceName,
                        id: out var id
                    ))
                {
                    // Retrieve doujin
                    var client = _clients.First(c => c.Name == sourceName);
                    var doujin = await client.GetAsync(id);

                    // Send zip to client
                    // TODO: Caching
                    using (var zip = new ZipArchive(response.OutputStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        foreach (var pageUrl in doujin.PageUrls)
                        {
                            // Create file in zip
                            var entry = zip.CreateEntry(Path.GetFileName(pageUrl), CompressionLevel.Optimal);

                            // Write page contents to entry
                            using (var dst = entry.Open())
                            using (var src = await _httpClient.GetStreamAsync(pageUrl))
                                await src.CopyToAsync(dst);
                        }
                    }
                    return;
                }
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        public void Dispose() => HttpListener.Close();
    }
}