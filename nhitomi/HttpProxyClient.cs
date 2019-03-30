using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

                return Proxies[_proxyIndex++ % Proxies.Count];
            }
        }

        public Task<HttpResponseMessage> GetAsync(
            string requestUrl,
            bool allowCache = false,
            CancellationToken cancellationToken = default)
        {
            var token = TokenGenerator.CreateToken(new TokenGenerator.ProxyGetPayload
                {
                    Url = requestUrl,
                    IsCached = allowCache
                },
                _settings.Discord.Token,
                serializer: _json);

            return Client.GetAsync($"{GetNextProxy().Url}/proxy/get?token={token}", cancellationToken);
        }
    }
}