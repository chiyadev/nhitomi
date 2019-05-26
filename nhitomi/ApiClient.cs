using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        readonly AppSettings _settings;
        readonly IHttpClient _httpClient;
        readonly JsonSerializer _serializer;
        readonly ILogger<ApiClient> _logger;

        public ApiClient(IOptions<AppSettings> options, IHttpClient httpClient, JsonSerializer serializer,
            ILogger<ApiClient> logger)
        {
            _settings = options.Value;
            _httpClient = httpClient;
            _serializer = serializer;
            _logger = logger;
        }

        HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            // create message
            var message = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(BaseUri, path.TrimStart('/'))
            };

            // authentication header
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Api.AuthToken);

            return message;
        }

        public async Task<DoujinClientInfo[]> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.SendAsync(
                CreateRequest(HttpMethod.Get, "doujins/sources"),
                cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                    return await _serializer.DeserializeAsync<DoujinClientInfo[]>(response.Content);

                throw new ApiException(response, "Could not retrieve doujin sources.");
            }
        }

        public async Task<Doujin> GetDoujinAsync(string source, string id,
            CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.SendAsync(
                CreateRequest(HttpMethod.Get, $"doujins/{source}/{id}"),
                cancellationToken))
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