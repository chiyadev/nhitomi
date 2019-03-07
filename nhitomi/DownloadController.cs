// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    [Route("dl")]
    public class DownloadController : ControllerBase
    {
        readonly AppSettings.DiscordSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly PhysicalCache _cache;
        readonly JsonSerializer _serializer;
        readonly ILogger _logger;

        public DownloadController(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer serializer,
            ILogger<DownloadController> logger
        )
        {
            _settings = options.Value.Discord;
            _clients = clients;
            _cache = new PhysicalCache(nameof(DownloadController));
            _serializer = serializer;
            _logger = logger;
        }

        [HttpGet("{*token}")]
        public async Task<ActionResult> GetAsync(string token)
        {
            if (!TokenGenerator.TryDeserializeToken(
                token: token,
                secret: _settings.Token,
                sourceName: out var sourceName,
                id: out var id
            ))
                return BadRequest();

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.First(c => c.Name == sourceName);
            var doujin = await client.GetAsync(id);

            return new FileCallbackResult(
                contentType: MediaTypeHeaderValue.Parse("application/zip"),
                callback: async (stream, context) =>
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        // Add doujin information file
                        addDoujinInfo(doujin, archive);

                        var pageUrls = doujin.PageUrls.ToArray();

                        for (var i = 0; i < pageUrls.Length; i++)
                        {
                            var pageUrl = pageUrls[i];

                            try
                            {
                                // Create file in zip
                                var entry = archive.CreateEntry(
                                    Path.GetFileNameWithoutExtension(pageUrl).PadLeft(3, '0') + Path.GetExtension(pageUrl),
                                    CompressionLevel.Optimal
                                );

                                Task<Stream> openSrcStream() => _cache.GetOrCreateStreamAsync(
                                    name: $"{doujin.Source.Name}/{doujin.Id}/{i}",
                                    getAsync: () => doujin.Source.GetStreamAsync(pageUrl)
                                );

                                // Write page contents to entry
                                using (var dst = entry.Open())
                                using (var src = await openSrcStream())
                                    await src.CopyToAsync(dst, context.HttpContext.RequestAborted);
                            }
                            catch (OperationCanceledException)
                            {
                                // Download was canceled. Whatever.
                                return;
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning(e, $"Exception while downloading `{pageUrl}`: {e.Message}");
                            }
                        }
                    }
                })
            {
                FileDownloadName = doujin.OriginalName + ".zip",
                LastModified = doujin.UploadTime
            };
        }

        void addDoujinInfo(IDoujin doujin, ZipArchive archive)
        {
            var infoEntry = archive.CreateEntry("_nhitomi.json", CompressionLevel.Optimal);

            using (var infoStream = infoEntry.Open())
            using (var infoWriter = new StreamWriter(infoStream))
                _serializer.Serialize(infoWriter, doujin);
        }
    }
}