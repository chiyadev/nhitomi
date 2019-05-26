using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi
{
    public class HttpClientWrapper : IHttpClient
    {
        public HttpClient Http { get; }

        public HttpClientWrapper(IHttpClientFactory httpClientFactory)
        {
            Http = httpClientFactory.CreateClient(nameof(HttpClientWrapper));
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken = default) =>
            Http.SendAsync(request, cancellationToken);
    }
}