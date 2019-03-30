// Copyright (c) 2018-2019 fate/loli
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Controllers
{
    public class ProxyController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly JsonSerializer _json;
        readonly ProxyList _proxies;
        readonly ILogger<ProxyController> _logger;

        public ProxyController(
            IOptions<AppSettings> options,
            JsonSerializer json,
            ProxyList proxies,
            ILogger<ProxyController> logger)
        {
            _settings = options.Value;
            _json = json;
            _proxies = proxies;
            _logger = logger;
        }

        [HttpPost("/download/proxies/register")]
        public ActionResult RegisterProxy([FromForm] string token)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyRegistrationPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid registration token.");

            lock (_proxies.Lock)
            {
                _proxies.Update();

                var proxy = _proxies.FirstOrDefault(p => p.Url == payload.ProxyUrl);

                if (proxy == null)
                {
                    _proxies.Add(proxy = new ProxyInfo
                    {
                        Url = payload.ProxyUrl
                    });

                    _logger.LogDebug($"Proxy '{proxy.Url}' registered.");
                }

                proxy.RegisterTime = DateTime.UtcNow;
                proxy.IPAddress = Request.HttpContext.Connection.RemoteIpAddress;

                return Ok($"Registered {proxy.Url}.");
            }
        }

        [HttpGet("/download/proxies/list")]
        public ActionResult GetProxies()
        {
            lock (_proxies.Lock)
            {
                _proxies.Update();

                var address = Request.HttpContext.Connection.RemoteIpAddress;
                var proxy = _proxies.FirstOrDefault(p => p.IPAddress.Equals(address));

                if (proxy == null)
                    return BadRequest("Not registered.");

                return Ok(_proxies.ToArray());
            }
        }
    }
}
