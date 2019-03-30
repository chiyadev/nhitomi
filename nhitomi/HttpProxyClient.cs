// Copyright (c) 2018-2019 fate/loli
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

        public ProxyInfo GetNextProxy()
        {
            lock (Proxies.Lock)
            {
                Proxies.Update();

                return Proxies.Count == 0
                    ? null
                    : Proxies[_proxyIndex++ % Proxies.Count];
            }
        }

        public Task<HttpResponseMessage> GetAsync(
            string requestUrl,
            bool allowCache = false,
            CancellationToken cancellationToken = default)
        {
            var proxy = GetNextProxy();

            if (proxy == null)
                return Client.GetAsync(requestUrl, cancellationToken);

            var token = TokenGenerator.CreateToken(new TokenGenerator.ProxyGetPayload
                {
                    Url = requestUrl,
                    IsCached = allowCache
                },
                _settings.Discord.Token,
                serializer: _json);

            return Client.GetAsync(
                $"{proxy.Url}/proxy/get?token={HttpUtility.UrlEncode(token)}",
                cancellationToken);
        }
    }
}