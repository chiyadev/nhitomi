using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public class HttpClientWrapper : IHttpClient
    {
        public HttpClient Http { get; }

        public HttpClientWrapper(HttpClient httpClient)
        {
            Http = httpClient;
        }

        public HttpClientWrapper(IHttpClientFactory httpClientFactory)
        {
            Http = httpClientFactory.CreateClient(nameof(HttpClientWrapper));
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                   CancellationToken cancellationToken = default) =>
            Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}