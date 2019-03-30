using System;
using System.IO;
using System.Net.Http;
using System.Threading;
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
    }
}
