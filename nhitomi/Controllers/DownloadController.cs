// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Controllers
{
    public class DownloadController : ControllerBase
    {
        static readonly string _downloadPage;
        const string _downloaderResourceId = "nhitomi.Controllers.DownloadClient.html";

        static DownloadController()
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(_downloaderResourceId))
            using (var reader = new StreamReader(stream))
                _downloadPage = reader.ReadToEnd();
        }

        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _json;
        readonly ProxyList _proxies;
        readonly ILogger<DownloadController> _logger;

        public DownloadController(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer json,
            ProxyList proxies,
            ILogger<DownloadController> logger)
        {
            _settings = options.Value;
            _clients = clients;
            _json = json;
            _proxies = proxies;
            _logger = logger;
        }

        [HttpGet("/download/{*token}")]
        public async Task<ActionResult> GetDownloaderAsync(string token)
        {
            if (!TokenGenerator.TryDeserializeDownloadToken(
                token, _settings.Discord.Token, out var source, out var id, out _, serializer: _json))
                return BadRequest("Download token has expired. Please try again.");

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.FindByName(source);
            var doujin = await client.GetAsync(id);

            if (doujin == null)
                return BadRequest("Doujin not found.");

            // Get proxies to be used by this download
            string[] proxies;

            lock (_proxies.Lock)
                proxies = _proxies.ActiveProxies.Select(p => p.Url).ToArray();

            // Create javascript downloader
            var downloader = _downloadPage.NamedFormat(new Dictionary<string, object>
            {
                {"token", token},
                {"title", doujin.PrettyName},
                {"subtitle", doujin.OriginalName ?? string.Empty},
                {"doujin", _json.Serialize(doujin)},
                {"proxies", _json.Serialize(proxies)}
            });

            return Content(downloader, "text/html");
        }

        [HttpPost("/download/proxies/register")]
        public ActionResult RegisterProxy([FromForm] string token)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyRegistrationPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid registration token.");

            lock (_proxies.Lock)
            {
                var proxy = _proxies.FirstOrDefault(p => p.Url == payload.ProxyUrl);

                if (proxy == null)
                    _proxies.Add(proxy = new ProxyInfo
                    {
                        Url = payload.ProxyUrl
                    });

                proxy.RegisterTime = DateTime.UtcNow;

                return Ok($"Registered {proxy.Url}.");
            }
        }
    }
}
