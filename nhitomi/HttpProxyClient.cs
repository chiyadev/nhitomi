// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class HttpProxyClient : IHttpProxyClient
    {
        readonly AppSettings _settings;
        readonly JsonSerializer _json;

        public HttpClient Client { get; }
        public ProxyList Proxies { get; }

        public HttpProxyClient(
            IOptions<AppSettings> options,
            JsonSerializer json,
            IHttpClientFactory httpFactory,
            ProxyList proxies)
        {
            _settings = options.Value;
            _json = json;

            Client = httpFactory.CreateClient(nameof(HttpProxyClient));
            Proxies = proxies;
        }

        int _proxyIndex;

        async Task<ProxyInfo> GetNextProxyAsync(CancellationToken cancellationToken = default)
        {
            await Proxies.Semaphore.WaitAsync(cancellationToken);
            try
            {
                return Proxies.Count == 0
                    ? null
                    : Proxies[_proxyIndex++ % Proxies.Count];
            }
            finally
            {
                Proxies.Semaphore.Release();
            }
        }

        public async Task<HttpResponseMessage> GetAsync(
            string requestUrl,
            bool allowCache = false,
            CancellationToken cancellationToken = default)
        {
            var proxy = await GetNextProxyAsync(cancellationToken);

            // fallback to direct access
            if (proxy == null)
                return await Client.GetAsync(requestUrl, cancellationToken);

            var token = TokenGenerator.CreateToken(new TokenGenerator.ProxyGetPayload
                {
                    Url = requestUrl,
                    IsCached = allowCache
                },
                _settings.Discord.Token,
                serializer: _json);

            return await Client.GetAsync(
                $"{proxy.Url}/proxy/get?token={HttpUtility.UrlEncode(token)}",
                cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsync(
            string requestUrl,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            var proxy = await GetNextProxyAsync(cancellationToken);

            // fallback to direct access
            if (proxy == null)
                return await Client.PostAsync(requestUrl, content, cancellationToken);

            var token = TokenGenerator.CreateToken(new TokenGenerator.ProxyPostPayload
                {
                    Url = requestUrl
                },
                _settings.Discord.Token,
                serializer: _json);

            return await Client.PostAsync(
                $"{proxy.Url}/proxy/post?token={HttpUtility.UrlEncode(token)}",
                content,
                cancellationToken);
        }
    }
}