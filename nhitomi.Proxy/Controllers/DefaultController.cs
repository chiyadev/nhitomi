// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Proxy.Services;
using Newtonsoft.Json;

namespace nhitomi.Proxy.Controllers
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly JsonSerializer _json;
        readonly CacheSyncService _caches;
        readonly ILogger<DefaultController> _logger;

        public DefaultController(
            IOptions<AppSettings> options,
            JsonSerializer json,
            CacheSyncService caches,
            ILogger<DefaultController> logger)
        {
            _settings = options.Value;
            _json = json;
            _caches = caches;
            _logger = logger;
        }

        [HttpGet]
        public string Get() => "nhitomi proxy server";

        [HttpPost("/proxy/list")]
        public ActionResult UpdateProxyList(
            [FromForm] string token,
            [FromForm] string urls)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyRegistrationPayload>(
                    token, _settings.Discord.Token, out var payload, serializer: _json) ||
                payload.ProxyUrl != _settings.Http.Url)
                return BadRequest("Invalid token.");

            using (var reader = new StringReader(urls))
            using (var jsonReader = new JsonTextReader(reader))
            {
                _caches.SyncProxies = _json.Deserialize<string[]>(jsonReader)?
                                          .Where(u => u != _settings.Http.Url)
                                          .ToArray()
                                      ?? new string[0];

                _caches.SyncProxiesUpdateTime = DateTime.Now;
            }

            _logger.LogDebug($"Updated proxy list: {urls}");

            return Ok();
        }
    }
}