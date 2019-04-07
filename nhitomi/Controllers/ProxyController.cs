// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task<ActionResult> RegisterProxyAsync(
            [FromForm] string token,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyRegistrationPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid registration token.");

            await _proxies.Semaphore.WaitAsync(cancellationToken);
            try
            {
                var proxy = _proxies.FirstOrDefault(p => p.Url == payload.ProxyUrl);

                if (proxy != null)
                    return BadRequest($"Proxy '{proxy.Url}' is already registered.");

                proxy = new ProxyInfo
                {
                    Url = payload.ProxyUrl,
                    IPAddress = Request.HttpContext.Connection.RemoteIpAddress,
                    RegistrationToken = token
                };

                _proxies.Add(proxy);

                return Ok($"Registered {proxy.Url}.");
            }
            finally
            {
                _proxies.Semaphore.Release();
            }
        }
    }
}