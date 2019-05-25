using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi.Proxy.Controllers
{
    public class CacheController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly JsonSerializer _json;
        readonly ILogger<CacheController> _logger;

        public CacheController(
            IOptions<AppSettings> options,
            JsonSerializer json,
            ILogger<CacheController> logger)
        {
            _settings = options.Value;
            _json = json;
            _logger = logger;
        }

        static readonly Dictionary<string, SemaphoreSlim> _uriSemaphores = new Dictionary<string, SemaphoreSlim>();

        public static SemaphoreSlim GetSemaphoreForUri(Uri uri)
        {
            lock (_uriSemaphores)
            {
                if (!_uriSemaphores.TryGetValue(uri.Authority, out var semaphore))
                    _uriSemaphores[uri.Authority] = semaphore = new SemaphoreSlim(1);

                return semaphore;
            }
        }

        public static Stream GetTemporaryStream() =>
            new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        public static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        public static string GetCachePath(Uri uri)
        {
            var basedir = Path.Combine(Path.GetTempPath(), "nhitomi");
            Directory.CreateDirectory(basedir);

            var filename = HashHelper.SHA256(uri.AbsoluteUri)
                .Replace('+', '-')
                .Replace('/', '_');

            return Path.Combine(basedir, filename);
        }

        [HttpPost("/proxy/cache")]
        public async Task<ActionResult> SetCacheAsync([FromQuery] string token)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxySetCachePayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            // write to temporary path first to not hog semaphore
            var cachePath = GetCachePath(uri);
            var tempPath = Path.GetTempFileName();

            using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                await Request.Body.CopyToAsync(dst, default(CancellationToken));

            await Semaphore.WaitAsync(default(CancellationToken));
            try
            {
                if (System.IO.File.Exists(cachePath))
                    System.IO.File.Delete(cachePath);

                System.IO.File.Move(tempPath, cachePath);

                await System.IO.File.WriteAllTextAsync(cachePath + ".contentType", Request.ContentType);

                return Created(new Uri("/proxy/get", UriKind.Relative), "Cache updated.");
            }
            finally
            {
                Semaphore.Release();

                _logger.LogDebug($"Received cache of '{payload.Url}'.");
            }
        }
    }
}