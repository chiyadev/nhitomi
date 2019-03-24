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
        readonly ILogger _logger;

        public ImageController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            ILogger<ImageController> logger
        )
        {
            _settings = options.Value.Discord;
            _http = httpFactory?.CreateClient(nameof(ImageController));
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

        static string getCachePath(Uri uri) =>
            Path.Combine(Path.GetTempPath(), "nhitomi", uri.Authority, uri.LocalPath);

        [HttpGet]
        public async Task<ActionResult> GetAsync(
            [FromQuery] string token,
            [FromQuery] string url,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken(token, _settings.Token, out var sourceName, out var id))
                return BadRequest("Invalid token.");

            if (!(Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                  uri.Scheme == "https" && uri.Host.Contains(sourceName, StringComparison.OrdinalIgnoreCase) &&
                  IsImage(uri)))
                return BadRequest("Invalid url.");

            _logger.LogDebug($"Received request: token {token}, url {url}");

            var mime = $"image/{Path.GetExtension(uri.LocalPath).TrimStart('.')}";
            var cachePath = getCachePath(uri);

            var semaphore = GetSemaphore(sourceName);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                Stream stream;

                try
                {
                    // this will fail if cache doesn't exist
                    stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read);
                }
                catch
                {
                    stream = new MemoryStream();

                    // we don't want image download to cancel when request cancels
                    using (var src = await _http.GetStreamAsync(uri))
                        await src.CopyToAsync(stream, default(CancellationToken));

                    stream.Position = 0;

                    // cache the image
                    using (var dst = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
                        await stream.CopyToAsync(dst, default(CancellationToken));

                    stream.Position = 0;
                }

                return File(stream, mime);
            }
            finally
            {
                // Rate limiting
                // todo: proper timing
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

                semaphore.Release();
            }
        }
    }
}