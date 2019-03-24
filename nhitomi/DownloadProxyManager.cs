using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DownloadProxyManager : BackgroundService
    {
        readonly AppSettings.DoujinSettings _settings;
        readonly HttpClient _http;
        readonly ILogger _logger;

        public DownloadProxyManager(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            ILogger<FeedUpdater> logger
        )
        {
            _settings = options.Value.Doujin;
            _http = httpFactory?.CreateClient(nameof(DownloadProxyManager));
            _logger = logger;
        }

        public IPAddress[] ProxyAddresses { get; private set; } = new string[0];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var proxies = new List<string>();

                for (var i = 0; i < _settings.MaxConcurrentProxies;)
                {
                    var proxy = _settings.DownloadProxies[i];

                    try
                    {
                        // check if proxy is online
                        using (var response = await _http.GetAsync(proxy, stoppingToken))
                        {
                            // try next proxy
                            if (!response.IsSuccessStatusCode)
                                continue;

                            proxies.Add(proxy);
                            i++;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Exception while contacting proxy '{proxy}'.");
                    }
                }

                // Make the new list of proxies available
                ProxyAddresses = proxies.ToArray();

                // Sleep
                await Task.Delay(TimeSpan.FromMinutes(_settings.ProxyCheckInterval), stoppingToken);
            }
        }
    }
}