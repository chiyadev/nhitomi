// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
        static readonly string _downloader;

        static DownloadController()
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("nhitomi.DownloadClient.html"))
            using (var reader = new StreamReader(stream))
                _downloader = reader.ReadToEnd();
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
            ILogger<DownloadController> logger
        )
        {
            _settings = options.Value.Discord;
            _clients = clients;
            _json = json;
            _proxyManager = proxyManager;
            _logger = logger;
        }

        [HttpGet("{*token}")]
        public async Task<ActionResult> GetAsync(string token)
        {
            if (!TokenGenerator.TryDeserializeDownloadToken(token, _settings.Token, out var sourceName, out var id))
                return BadRequest("Download token has expired. Please try again.");

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.First(c => c.Name == sourceName);
            var doujin = await client.GetAsync(id);

            if (doujin == null)
                return BadRequest("Doujin not found.");

            // Create javascript downloader
            var downloader = _downloader.NamedFormat(new Dictionary<string, object>
            {
                {"title", doujin.PrettyName},
                {"subtitle", doujin.OriginalName ?? string.Empty},
                {"proxyList", string.Join("\",\"", _proxyManager.ProxyAddresses)},
                {"token", token},
                {"doujin", HttpUtility.JavaScriptStringEncode(_json.Serialize(doujin))}
            });

            return Content(downloader, "text/html");
        }
    }
}
