// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        static DownloadController()
        {
            using (var stream = typeof(Program).Assembly
                .GetManifestResourceStream("nhitomi.Controllers.DownloadClient.html"))
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

        [HttpGet("/download")]
        public async Task<ActionResult> GetDownloaderAsync([FromQuery] string token)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyDownloadPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (DateTime.UtcNow >= payload.Expires)
                return BadRequest("Download token has expired. Please try again.");

            _logger.LogDebug($"Received download request: token {token}");

            // retrieve doujin
            var client = _clients.FindByName(payload.Source);
            var doujin = await client.GetAsync(payload.Id);

            if (doujin == null)
                return NotFound("Doujin not found.");

            // get proxies to be used by this download
            string[] proxies;

            lock (_proxies.Lock)
            {
                _proxies.Update();
                proxies = _proxies.Select(p => p.Url).ToArray();
            }

            try
            {
                // generate download page
                var downloader = _downloadPage.NamedFormat(new Dictionary<string, object>
                {
                    {"token", token},
                    {"title", doujin.OriginalName ?? doujin.PrettyName},
                    {"subtitle", doujin.OriginalName == doujin.PrettyName ? string.Empty : doujin.PrettyName},
                    {"doujin", _json.Serialize(doujin)},
                    {"proxies", _json.Serialize(proxies)}
                });

                return Content(downloader, "text/html");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while generating download client for {payload.Source}/{payload.Id}");

                return StatusCode(500, "Internal server error while generating downloader.");
            }
        }
    }
}
