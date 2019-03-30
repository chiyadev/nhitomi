// Copyright (c) 2018-2019 fate/loli
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Proxy.Services;
using Newtonsoft.Json;

namespace nhitomi.Proxy.Controllers
{
    public class ProxyController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly CacheSyncService _caches;
        readonly ILogger<ProxyController> _logger;

        public ProxyController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            CacheSyncService caches,
            ILogger<ProxyController> logger)
        {
            _settings = options.Value;
            _http = httpFactory?.CreateClient(nameof(ProxyController));
            _json = json;
            _caches = caches;
            _logger = logger;
        }

        static readonly Dictionary<string, SemaphoreSlim> _uriSemaphores = new Dictionary<string, SemaphoreSlim>();

        static SemaphoreSlim GetSemaphoreForUri(Uri uri)
        {
            lock (_uriSemaphores)
            {
                if (!_uriSemaphores.TryGetValue(uri.Authority, out var semaphore))
                    _uriSemaphores[uri.Authority] = semaphore = new SemaphoreSlim(1);

                return semaphore;
            }
        }

        const string _mime = "application/octet-stream";

        [HttpGet("/proxy/get")]
        public async Task<ActionResult> GetAsync(
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyGetPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            return await GetAsync(uri, payload.IsCached, null, cancellationToken);
        }

        // this endpoint is used by the downloader script
        [HttpGet("/proxy/image")]
        public async Task<ActionResult> GetImageAsync(
            [FromQuery] string url,
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyDownloadPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            if (DateTime.UtcNow >= payload.Expires)
                return BadRequest("Token expired.");

            return await GetAsync(uri, true, payload.RequestThrottle, cancellationToken);
        }

        async Task<ActionResult> GetAsync(
            Uri uri,
            bool cached,
            double? throttle = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cachePath = CacheController.GetCachePath(uri);

                if (cached)
                {
                    // try finding from cache
                    await CacheController.Semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (System.IO.File.Exists(cachePath))
                        {
                            _logger.LogDebug($"Found '{uri}' from cache.");

                            // copy to temporary path for faster transfer
                            var tempPath = Path.GetTempFileName();

                            System.IO.File.Copy(cachePath, tempPath, true);

                            return File(new FileStream(tempPath, FileMode.Open), _mime);
                        }
                    }
                    finally
                    {
                        CacheController.Semaphore.Release();
                    }
                }

                var memory = new MemoryStream();

                // download data to memory
                // semaphore is used to rate limit requests
                var semaphore = GetSemaphoreForUri(uri);

                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using (var src = await _http.GetStreamAsync(uri))
                        await src.CopyToAsync(memory, default(CancellationToken));

                    memory.Position = 0;

                    _logger.LogDebug($"Downloaded '{uri}'.");
                }
                finally
                {
                    // rate limiting
                    if (throttle.HasValue)
                        await Task.Delay(TimeSpan.FromMilliseconds(throttle.Value), default);

                    semaphore.Release();
                }

                if (cached)
                {
                    // write to temporary path first to not hog semaphore
                    var tempPath = Path.GetTempFileName();

                    using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        await memory.CopyToAsync(dst, default(CancellationToken));

                    memory.Position = 0;

                    // cache the data to disk
                    await CacheController.Semaphore.WaitAsync(default(CancellationToken));
                    try
                    {
                        if (System.IO.File.Exists(cachePath))
                            System.IO.File.Delete(cachePath);

                        System.IO.File.Move(tempPath, cachePath);

                        _logger.LogDebug($"Cached '{uri}' to disk.");
                    }
                    finally
                    {
                        CacheController.Semaphore.Release();
                    }

                    // enqueue cache to be synced
                    _caches.SyncQueue.Enqueue(uri);
                }

                return File(memory, _mime);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while downloading '{uri}'.");

                return StatusCode(500, e);
            }
        }
    }
}
