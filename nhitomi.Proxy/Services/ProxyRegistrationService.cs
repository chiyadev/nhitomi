// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Proxy.Services
{
    public class ProxyRegistrationService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly JsonSerializer _json;
        readonly HttpClient _client;
        readonly CacheSyncService _caches;
        readonly ILogger<ProxyRegistrationService> _logger;

        public ProxyRegistrationService(
            IOptions<AppSettings> options,
            JsonSerializer json,
            IHttpClientFactory httpFactory,
            CacheSyncService caches,
            ILogger<ProxyRegistrationService> logger)
        {
            _logger = logger;
            _json = json;
            _client = httpFactory.CreateClient(nameof(ProxyRegistrationService));
            _caches = caches;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_caches.SyncProxiesUpdateTime.AddMinutes(5) >= DateTime.Now)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                try
                {
                    // generate token used to register this proxy
                    var token = TokenGenerator.CreateToken(
                        new TokenGenerator.ProxyRegistrationPayload
                        {
                            ProxyUrl = _settings.Http.Url
                        },
                        _settings.Discord.Token, serializer: _json);

                    _logger.LogDebug($"Registration token: {token}");

                    // endpoint: /download/proxies/register
                    using (var response = await _client.PostAsync(
                        $"{_settings.Http.MainUrl}/download/proxies/register",
                        new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            {"token", token}
                        }), stoppingToken))
                    {
                        var message = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning(
                                $"Could not register as proxy at {response.RequestMessage.RequestUri}: {message}");
                        }
                        else
                        {
                            _logger.LogDebug($"Proxy registration success: {message}");

                            _caches.SyncProxies = new string[0];
                            _caches.SyncProxiesUpdateTime = DateTime.Now;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Exception while registering proxy.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
