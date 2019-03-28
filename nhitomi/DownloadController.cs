// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class DownloadController : ControllerBase
    {
        static readonly string _downloadPage;

        static DownloadController()
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("nhitomi.DownloadClient.html"))
            using (var reader = new StreamReader(stream))
                _downloadPage = reader.ReadToEnd();
        }

        readonly AppSettings.DiscordSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _json;
        readonly DownloadProxyManager _proxyManager;
        readonly ILogger _logger;

        public DownloadController(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer json,
            DownloadProxyManager proxyManager,
            ILogger<DownloadController> logger)
        {
            _settings = options.Value.Discord;
            _clients = clients;
            _json = json;
            _proxyManager = proxyManager;
            _logger = logger;
        }

        [HttpGet("/download/{*token}")]
        public async Task<ActionResult> GetDownloaderAsync(string token)
        {
            if (!TokenGenerator.TryDeserializeDownloadToken(
                token, _settings.Token, out var sourceName, out var id, out _, serializer: _json))
                return BadRequest("Download token has expired. Please try again.");

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.First(c => c.Name == sourceName);
            var doujin = await client.GetAsync(id);

            if (doujin == null)
                return BadRequest("Doujin not found.");

            // Create javascript downloader
            var downloader = _downloadPage.NamedFormat(new Dictionary<string, object>
            {
                {"token", token},
                {"title", doujin.PrettyName},
                {"subtitle", doujin.OriginalName ?? string.Empty},
                {"doujin", _json.Serialize(doujin)},
                {"proxies", _json.Serialize(_proxyManager.ProxyUrls)}
            });

            return Content(downloader, "text/html");
        }
    }
}
