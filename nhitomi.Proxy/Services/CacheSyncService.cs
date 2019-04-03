using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Proxy.Controllers;
using Newtonsoft.Json;

namespace nhitomi.Proxy.Services
{
    public class CacheSyncService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger<CacheSyncService> _logger;

        public CacheSyncService(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<CacheSyncService> logger)
        {
            _settings = options.Value;
            _http = httpFactory.CreateClient(nameof(CacheSyncService));
            _json = json;
            _logger = logger;
        }

        public readonly ConcurrentQueue<Uri> SyncQueue = new ConcurrentQueue<Uri>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (SyncQueue.TryDequeue(out var uri))
                        await SyncCacheAsync(uri, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while syncing caches.");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        async Task SyncCacheAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            string contentType;

            // copy to temporary path for faster transfer
            var cachePath = CacheController.GetCachePath(uri);
            var tempPath = Path.GetTempFileName();

            await CacheController.Semaphore.WaitAsync(cancellationToken);
            try
            {
                File.Copy(cachePath, tempPath, true);

                contentType = await File.ReadAllTextAsync(cachePath + ".contentType", cancellationToken);
            }
            finally
            {
                CacheController.Semaphore.Release();
            }

            var token = TokenGenerator.CreateToken(new TokenGenerator.ProxySetCachePayload
                {
                    Url = uri.AbsoluteUri
                },
                _settings.Discord.Token,
                serializer: _json);

            foreach (var proxyUrl in await GetSyncProxiesAsync(cancellationToken))
            {
                try
                {
                    using (var content = new StreamContent(new FileStream(tempPath, FileMode.Open)))
                    {
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

                        using (var response = await _http.PostAsync(
                            $"{proxyUrl}/proxy/cache?token={HttpUtility.UrlEncode(token)}",
                            content,
                            cancellationToken))
                        {
                            if (response.IsSuccessStatusCode)
                                _logger.LogDebug($"Synced cache of '{uri}' with '{proxyUrl}'.");
                            else
                                _logger.LogWarning($"Could not sync cache of '{uri}' with '{proxyUrl}': " +
                                                   await response.Content.ReadAsStringAsync());
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Could not sync cache of '{uri}' with '{proxyUrl}'.");
                }
            }

            File.Delete(tempPath);
        }

        DateTime _syncProxiesUpdateTime;
        string[] _syncProxies = new string[0];

        async Task<string[]> GetSyncProxiesAsync(CancellationToken cancellationToken = default)
        {
            if (_syncProxiesUpdateTime.AddMinutes(10) >= DateTime.Now)
                return _syncProxies;

            using (var response = await _http.GetAsync(
                $"{_settings.Http.MainUrl}/download/proxies/list", cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Could not get list of proxies: {await response.Content.ReadAsStringAsync()}");

                    _syncProxiesUpdateTime = DateTime.MinValue;
                    return _syncProxies = new string[0];
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    _syncProxiesUpdateTime = DateTime.Now;
                    _syncProxies = _json
                        .Deserialize<ProxyInfo[]>(jsonReader)
                        .Select(p => p.Url)
                        .Where(u => u != _settings.Http.Url)
                        .ToArray();
                }

                _logger.LogDebug($"Found proxies to sync with: {string.Join(", ", _syncProxies)}");
                return _syncProxies;
            }
        }
    }
}
