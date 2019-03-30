using System;
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
    public class CacheController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly ILogger<CacheController> _logger;

        public CacheController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            ILogger<CacheController> logger)
        {
            _settings = options.Value;
            _http = httpFactory?.CreateClient(nameof(CacheController));
            _json = json;
            _logger = logger;
        }

        public static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        public static string GetCachePath(Uri uri)
        {
            var path = Path.Combine(Path.GetTempPath(), "nhitomi", HashHelper.SHA256(uri.AbsoluteUri));

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            return path;
        }

        [HttpPost("/proxy/cache")]
        public async Task<ActionResult> SetCacheAsync(
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
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

                return Created(new Uri("/proxy/get", UriKind.Relative), "Cache updated.");
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
