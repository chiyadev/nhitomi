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
    [Route("/proxy/image")]
    public class ImageController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public ImageController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<ImageController> logger)
        {
            _settings = options.Value;
            _http = httpFactory?.CreateClient(nameof(ImageController));
            _json = json;
            _logger = logger;
        }

        static readonly SemaphoreSlim _cacheSemaphore = new SemaphoreSlim(1);
        static readonly Dictionary<string, SemaphoreSlim> _semaphores = new Dictionary<string, SemaphoreSlim>();

        static SemaphoreSlim getSemaphore(string name)
        {
            lock (_semaphores)
            {
                if (!_semaphores.TryGetValue(name, out var semaphore))
                    _semaphores[name] = semaphore = new SemaphoreSlim(1);

                return semaphore;
            }
        }

        static string getCachePath(Uri uri)
        {
            var path = Path.Combine(Path.GetTempPath(), "nhitomi", uri.Authority + uri.LocalPath)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
        }

        [HttpGet]
        public async Task<ActionResult> GetAsync(
            [FromQuery] string url,
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.DownloadPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            if (DateTime.UtcNow >= payload.Expires)
                return BadRequest("Token expired.");

            _logger.LogDebug($"Received request: token {token}, url {url}");

            var cachePath = getCachePath(uri);
            const string mime = "application/octet-stream";

            await _cacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                // try finding from cache
                if (System.IO.File.Exists(cachePath))
                    return File(new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read), mime);
            }
            finally
            {
                _cacheSemaphore.Release();
            }

            var memory = new MemoryStream();

            // download image
            // semaphore is used to rate limit requests
            var semaphore = getSemaphore(uri.Authority);
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using (var src = await _http.GetStreamAsync(uri))
                    await src.CopyToAsync(memory, default(CancellationToken));

                memory.Position = 0;
            }
            finally
            {
                // Rate limiting
                // todo: proper timing
                await Task.Delay(TimeSpan.FromMilliseconds(payload.RequestThrottle), default);

                semaphore.Release();
            }

            // cache the downloaded image to disk
            await _cacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                using (var dst = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await memory.CopyToAsync(dst, default(CancellationToken));

                memory.Position = 0;
            }
            finally
            {
                _cacheSemaphore.Release();
            }

            return File(memory, mime);
        }
    }
}
