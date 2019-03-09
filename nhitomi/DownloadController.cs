// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
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
            if (!TokenGenerator.TryDeserializeToken(token, _settings.Token, out var sourceName, out var id))
                return BadRequest();

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.First(c => c.Name == sourceName);
            var doujin = await client.GetAsync(id);
        }
    }
}
