using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Services
{
    public class ProxyListBroadcastService : BackgroundService
    {
        readonly ProxyList _proxies;
        readonly HttpClient _http;
        readonly JsonSerializer _json;

        public ProxyListBroadcastService(
            ProxyList proxies,
            IHttpClientFactory httpFactory,
            JsonSerializer json)
        {
            _proxies = proxies;
            _http = httpFactory.CreateClient(nameof(ProxyListBroadcastService));
            _json = json;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _proxies.Semaphore.WaitAsync(stoppingToken);
                try
                {
                    var urls = _json.Serialize(_proxies.Select(p => p.Url));

                    foreach (var proxy in _proxies.ToArray())
                        try
                        {
                            await _http.PostAsync($"{proxy.Url}/proxy/list",
                                new FormUrlEncodedContent(new Dictionary<string, string>
                                {
                                    {"token", proxy.RegistrationToken},
                                    {"urls", urls}
                                }), stoppingToken);
                        }
                        catch
                        {
                            _proxies.Remove(proxy);
                        }
                }
                finally
                {
                    _proxies.Semaphore.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}