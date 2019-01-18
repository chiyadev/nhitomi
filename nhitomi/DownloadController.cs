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
using Newtonsoft.Json;

namespace nhitomi
{
    [Route("dl")]
    public class DownloadController : ControllerBase
    {
        readonly AppSettings.DiscordSettings _settings;
        readonly ISet<IDoujinClient> _clients;
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
                    // TODO: downloaded image caching
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        // Add doujin information file
                        await addDoujinInfoAsync(doujin, archive);

                        foreach (var pageUrl in doujin.PageUrls)
                            try
                            {
                                if (context.HttpContext.RequestAborted.IsCancellationRequested)
                                    break;

                                // Create file in zip
                                var entry = archive.CreateEntry(
                                    Path.GetFileNameWithoutExtension(pageUrl).PadLeft(3, '0') + Path.GetExtension(pageUrl),
                                    CompressionLevel.Optimal
                                );

                                // Write page contents to entry
                                using (var dst = entry.Open())
                                using (var src = await doujin.Source.GetStreamAsync(pageUrl))
                                    await src.CopyToAsync(dst, context.HttpContext.RequestAborted);
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning(e, $"Exception while downloading `{pageUrl}`: {e.Message}");
                            }
                    }
                })
            {
                FileDownloadName = doujin.OriginalName + ".zip",
                LastModified = doujin.UploadTime
            };
        }

        async Task addDoujinInfoAsync(IDoujin doujin, ZipArchive archive)
        {
            var infoEntry = archive.CreateEntry("_nhitomi.json", CompressionLevel.Optimal);

            using (var infoStream = infoEntry.Open())
            using (var infoWriter = new StreamWriter(infoStream))
                _serializer.Serialize(infoWriter, doujin);
        }
    }
}