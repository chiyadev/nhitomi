using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Core
{
    public interface IHttpClient
    {
        HttpClient Http { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
}