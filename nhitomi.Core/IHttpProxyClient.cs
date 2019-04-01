using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public interface IHttpProxyClient
    {
        HttpClient Client { get; }
        ProxyList Proxies { get; }

        ProxyInfo GetNextProxy();

        Task<HttpResponseMessage> GetAsync(
            string requestUrl,
            bool allowCache = false,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> PostAsync(
            string requestUrl,
            HttpContent content,
            CancellationToken cancellationToken = default);
    }
}
