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
    [Route("/proxy")]
    public class ProxyController : ControllerBase
    {
        readonly AppSettings.DiscordSettings _settings;
        readonly PhysicalCache _cache;
        readonly HttpClient _http;
        readonly ILogger _logger;

        public ProxyController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<ProxyController> logger
        )
        {
            _settings = options.Value.Discord;
            _http = httpFactory?.CreateClient(nameof(ProxyController));
            _cache = new PhysicalCache(nameof(ProxyController), json);
            _logger = logger;
        }

        static readonly Dictionary<string, SemaphoreSlim> _semaphores = new Dictionary<string, SemaphoreSlim>();

        static SemaphoreSlim GetSemaphore(string name)
        {
            if (!_semaphores.TryGetValue(name, out var semaphore))
                _semaphores[name] = semaphore = new SemaphoreSlim(1);

            return semaphore;
        }

        static bool IsImage(Uri uri)
        {
            switch (Path.GetExtension(uri.LocalPath))
            {
                case ".tif":
                case ".tiff":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".png":
                    return true;

                default:
                    return false;
            }
        }

        [HttpGet("image")]
        public async Task<ActionResult> GetImageAsync(
            [FromQuery] string token,
            [FromQuery] string url,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken(token, _settings.Token, out var sourceName, out var id))
                return BadRequest("Invalid token.");

            if (!(Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                  uri.Scheme == "https" && uri.Host.Contains(sourceName) && IsImage(uri)))
                return BadRequest("Invalid url.");

            _logger.LogDebug($"Received request: token {token}");

            var semaphore = GetSemaphore(sourceName);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Rate limiting
                // todo: proper timing
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

                var stream = await _cache.GetOrCreateStreamAsync(
                    uri.Authority + uri.LocalPath,
                    () => _http.GetStreamAsync(uri));

                var mime = $"image/{Path.GetExtension(uri.LocalPath).TrimStart('.')}";

                return File(stream, mime);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}