// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class DownloadServer : IDisposable
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _serializer;
        readonly ILogger _logger;

        public HttpListener HttpListener { get; }

        public DownloadServer(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer serializer,
            ILogger<DownloadServer> logger
        )
        {
            _settings = options.Value;
            _clients = clients;
            _serializer = serializer;
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
                var token = match.Groups["token"].Value;

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

                    // Response headers
                    response.ContentType = MediaTypeNames.Application.Zip;
                    response.AddHeader("Content-disposition", $"attachment; filename*=UTF-8''{Uri.EscapeDataString(doujin.OriginalName + ".zip")}");

                    // Send zip to client
                    // TODO: Caching
                    using (var zip = new ZipArchive(response.OutputStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        // Add doujin information file
                        var infoEntry = zip.CreateEntry("_nhitomi.json", CompressionLevel.Optimal);

                        using (var infoStream = infoEntry.Open())
                        using (var infoWriter = new StreamWriter(infoStream))
                            _serializer.Serialize(infoWriter, doujin);

                        foreach (var pageUrl in doujin.PageUrls)
                            try
                            {
                                // Create file in zip
                                var entry = zip.CreateEntry(
                                    Path.GetFileNameWithoutExtension(pageUrl).PadLeft(3, '0') + Path.GetExtension(pageUrl),
                                    CompressionLevel.Optimal
                                );

                                // Write page contents to entry
                                using (var dst = entry.Open())
                                using (var src = await doujin.Source.GetStreamAsync(pageUrl))
                                    await src.CopyToAsync(dst);
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning(e, $"Exception while downloading `{pageUrl}`: {e.Message}");
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