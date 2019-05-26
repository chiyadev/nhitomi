using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public interface IApiClient
    {
        Task<DoujinClientInfo[]> GetSourcesAsync(CancellationToken cancellationToken = default);
        Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default);
    }

    public class ApiClient : IApiClient
    {
        static Uri BaseUri { get; } = new Uri("https://nhitomi.chiya.dev/api");

        readonly IHttpClient _httpClient;
        readonly JsonSerializer _serializer;
        readonly ILogger<ApiClient> _logger;

        public ApiClient(IHttpClient httpClient, JsonSerializer serializer, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _serializer = serializer;
            _logger = logger;
        }

        public async Task<DoujinClientInfo[]> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(BaseUri, "doujins/sources")
            }, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                    return await _serializer.DeserializeAsync<DoujinClientInfo[]>(response.Content);

                throw new ApiException(response, "Could not retrieve doujin sources.");
            }
        }

        public async Task<Doujin> GetDoujinAsync(string source, string id,
            CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(BaseUri, $"doujins/{source}/{id}")
            }, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                    return await _serializer.DeserializeAsync<Doujin>(response.Content);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                throw new ApiException(response, $"Could not get doujin '{source}/{id}'");
            }
        }
    }

    [Serializable]
    public class ApiException : Exception
    {
        public ApiException(HttpResponseMessage response, string message)
            : base($"{message} ({response.StatusCode}: {response.ReasonPhrase ?? "Unknown reason"})")
        {
        }

        protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}