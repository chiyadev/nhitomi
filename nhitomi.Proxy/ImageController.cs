// Copyright (c) 2018-2019 phosphene47
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

namespace nhitomi.Proxy
{
    [Route("/proxy/image")]
    public class ImageController : ControllerBase
    {
        readonly AppSettings.DiscordSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public ImageController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<ImageController> logger
        )
        {
            _settings = options.Value.Discord;
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
            if (!TokenGenerator.TryDeserializeDownloadToken(
                token, _settings.Token, out _, out _, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            _logger.LogDebug($"Received request: token {token}, url {url}");

            var cachePath = getCachePath(uri);

            Stream stream;

            try
            {
                await _cacheSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // try finding from cache
                    // this will fail if cache doesn't exist
                    stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                finally
                {
                    _cacheSemaphore.Release();
                }
            }
            catch
            {
                stream = new MemoryStream();

                // download image
                // semaphore is used to rate limit requests to remote hosts
                var semaphore = getSemaphore(uri.Authority);
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using (var src = await _http.GetStreamAsync(uri))
                        // we don't want image download to cancel when request cancels
                        await src.CopyToAsync(stream, default(CancellationToken));
                }
                finally
                {
                    // Rate limiting
                    // todo: proper timing
                    await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

                    semaphore.Release();
                }

                stream.Position = 0;

                await _cacheSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // cache the image
                    using (var dst = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
                        await stream.CopyToAsync(dst, default(CancellationToken));
                }
                finally
                {
                    _cacheSemaphore.Release();
                }

                stream.Position = 0;
            }

            return File(stream, "application/octet-stream");
        }
    }
}