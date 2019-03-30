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
using Newtonsoft.Json;

namespace nhitomi.Proxy.Controllers
{
    public class ProxyController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger<ProxyController> _logger;

        public ProxyController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<ProxyController> logger)
        {
            _settings = options.Value;
            _http = httpFactory?.CreateClient(nameof(ProxyController));
            _json = json;
            _logger = logger;
        }

        static readonly SemaphoreSlim _cacheSemaphore = new SemaphoreSlim(1);
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

        static string GetCachePath(Uri uri)
        {
            var path = Path.Combine(Path.GetTempPath(), "nhitomi", HashHelper.SHA256(uri.AbsoluteUri));

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
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
                var cachePath = GetCachePath(uri);

                if (cached)
                {
                    // try finding from cache
                    await _cacheSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (System.IO.File.Exists(cachePath))
                        {
                            _logger.LogDebug($"Found '{uri}' from cache.");

                            return File(
                                new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                                _mime);
                        }
                    }
                    finally
                    {
                        _cacheSemaphore.Release();
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
                    // cache the data to disk
                    await _cacheSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        using (var dst = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            await memory.CopyToAsync(dst, default(CancellationToken));

                        memory.Position = 0;

                        _logger.LogDebug($"Cached '{uri}' to disk.");
                    }
                    finally
                    {
                        _cacheSemaphore.Release();
                    }
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