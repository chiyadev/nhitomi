// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public class PostController : ControllerBase
    {
        readonly AppSettings _settings;
        readonly HttpClient _http;
        readonly JsonSerializer _json;
        readonly CacheSyncService _caches;
        readonly ILogger<PostController> _logger;

        public PostController(
            IOptions<AppSettings> options,
            IHttpClientFactory httpFactory,
            JsonSerializer json,
            CacheSyncService caches,
            ILogger<PostController> logger)
        {
            _settings = options.Value;
            _http = httpFactory?.CreateClient(nameof(PostController));
            _json = json;
            _caches = caches;
            _logger = logger;
        }

        [HttpPost("/proxy/post")]
        public async Task<ActionResult> PostAsync(
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            if (!TokenGenerator.TryDeserializeToken<TokenGenerator.ProxyPostPayload>(
                token, _settings.Discord.Token, out var payload, serializer: _json))
                return BadRequest("Invalid token.");

            if (!Uri.TryCreate(payload.Url, UriKind.Absolute, out var uri))
                return BadRequest("Invalid URL.");

            string contentType;

            var tempStream = CacheController.GetTemporaryStream();

            // load request data to a temporary stream
            await Request.Body.CopyToAsync(tempStream, cancellationToken);

            tempStream.Position = 0;

            var semaphore = CacheController.GetSemaphoreForUri(uri);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using (var content = new StreamContent(tempStream))
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);

                    using (var response = await _http.PostAsync(uri, content, cancellationToken))
                    {
                        if (!response.IsSuccessStatusCode)
                            return StatusCode((int) response.StatusCode, response.ReasonPhrase);

                        contentType = response.Content.Headers.ContentType.ToString();

                        // load response data to another temporary stream
                        tempStream = CacheController.GetTemporaryStream();

                        using (var src = await response.Content.ReadAsStreamAsync())
                            await src.CopyToAsync(tempStream, default(CancellationToken));
                    }
                }

                tempStream.Position = 0;

                _logger.LogDebug($"Downloaded '{uri}'.");
            }
            finally
            {
                semaphore.Release();
            }

            return File(tempStream, contentType);
        }
    }
}
