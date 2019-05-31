using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class DownloadToken
    {
        readonly string _source;
        readonly string _id;
        readonly string _value;
        readonly Func<string, Uri> _getUri;

        public DownloadToken(string source, string id, string value, Func<string, Uri> getUri)
        {
            _source = source;
            _id = id;
            _value = value;
            _getUri = getUri;
        }

        public override string ToString() => _value;

        public Uri GetUri(int pageIndex) => _getUri($"{_source}/{_id}/{pageIndex}?token={_value}");
    }

    public interface IApiClient
    {
        Task LoginAsync(CancellationToken cancellationToken = default);

        Task<DownloadToken> CreateDownloadAsync(Doujin doujin, CancellationToken cancellationToken = default);
    }

    public class ApiClient : IApiClient
    {
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

        Uri GetRequestUri(string path) => new Uri($"{_settings.Api.BaseUrl}/v1/{path}");

        string _accessToken;

        async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, object body = null,
            CancellationToken cancellationToken = default, bool ensureAuth = true)
        {
            var message = new HttpRequestMessage
            {
                Method = method,
                RequestUri = GetRequestUri(path)
            };

            // authentication
            var accessToken = _accessToken;

            if (accessToken != null)
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // request body
            if (body != null)
            {
                message.Content = new StringContent(
                    _serializer.Serialize(body),
                    Encoding.Default,
                    "application/json");
            }

            // send request
            var response = await _httpClient.SendAsync(message, cancellationToken);

            if (ensureAuth && response.StatusCode == HttpStatusCode.Forbidden)
            {
                // access token may have expired
                await LoginAsync(cancellationToken);

                // retry this request
                response = await RequestAsync(method, path, body, cancellationToken, false);
            }

            return response;
        }

        sealed class LoginRequest
        {
            [JsonProperty("id")] public string Id { get; set; }
        }

        sealed class LoginResult
        {
            [JsonProperty("user")] public UserInfo User { get; set; }
            [JsonProperty("token")] public string Token { get; set; }

            public class UserInfo
            {
                public Guid Id { get; set; }
                public string Name { get; set; }
            }
        }

        readonly SemaphoreSlim _loginSemaphore = new SemaphoreSlim(1);

        public async Task LoginAsync(CancellationToken cancellationToken = default)
        {
            await _loginSemaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Logging in to API...");

                using (var response = await RequestAsync(HttpMethod.Post, "users/auth", new LoginRequest
                {
                    // contact phosphene47#0001 for an api token
                    Id = _settings.Api.AuthToken
                }, cancellationToken, false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new ApiClientException(
                            $"API authentication failed: {response.ReasonPhrase ?? "no reason"}");

                    var result = _serializer.Deserialize<LoginResult>(await response.Content.ReadAsStringAsync());

                    // remember token to be used for later requests
                    _accessToken = result.Token;
                }
            }
            finally
            {
                _loginSemaphore.Release();
            }
        }

        sealed class CreateDownloadRequest
        {
            [JsonProperty("source")] public string Source { get; set; }
            [JsonProperty("id")] public string Id { get; set; }
        }

        sealed class CreateDownloadResult
        {
            [JsonProperty("doujin")] public Doujin Doujin { get; set; }
            [JsonProperty("token")] public string Token { get; set; }
        }

        public async Task<DownloadToken> CreateDownloadAsync(Doujin doujin,
            CancellationToken cancellationToken = default)
        {
            using (var response = await RequestAsync(HttpMethod.Post, "dl", new CreateDownloadRequest
            {
                Source = doujin.Source,
                Id = doujin.SourceId
            }, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = _serializer.Deserialize<CreateDownloadResult>(await response.Content.ReadAsStringAsync());

                return new DownloadToken(doujin.Source, doujin.SourceId, result.Token, GetRequestUri);
            }
        }
    }

    [Serializable]
    public class ApiClientException : Exception
    {
        public ApiClientException()
        {
        }

        public ApiClientException(string message) : base(message)
        {
        }

        public ApiClientException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ApiClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}