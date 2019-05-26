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

        public async Task<Doujin> GetDoujinAsync(string source, string id,
            CancellationToken cancellationToken = default)
        {
            using (var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(BaseUri, $"doujins/{source}/{id}")
            }, cancellationToken))
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        using (var stream = await response.Content.ReadAsStreamAsync())
                            return _serializer.Deserialize<Doujin>(stream);

                    case HttpStatusCode.NotFound:
                        return null;

                    default:
                        throw new ApiException(response, $"Could not get doujin '{source}/{id}'");
                }
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