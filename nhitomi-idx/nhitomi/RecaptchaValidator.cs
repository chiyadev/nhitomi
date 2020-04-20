using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace nhitomi
{
    public class RecaptchaOptions
    {
        /// <summary>
        /// Google reCAPTCHA site key.
        /// </summary>
        public string SiteKey { get; set; }

        /// <summary>
        /// Google reCAPTCHA secret key.
        /// </summary>
        public string SecretKey { get; set; }
    }

    public interface IRecaptchaValidator
    {
        bool Enabled { get; }

        Task<bool> TryValidateAsync(string token, CancellationToken cancellationToken = default);
    }

    public class RecaptchaValidator : IRecaptchaValidator
    {
        readonly IOptionsMonitor<RecaptchaOptions> _options;
        readonly HttpClient _http;
        readonly ILogger<RecaptchaValidator> _logger;

        public bool Enabled
        {
            get
            {
                var options = _options.CurrentValue;

                return options.SiteKey != null && options.SecretKey != null;
            }
        }

        public RecaptchaValidator(IOptionsMonitor<RecaptchaOptions> options, IHttpClientFactory httpClientFactory, ILogger<RecaptchaValidator> logger)
        {
            _options = options;
            _http    = httpClientFactory.CreateClient(nameof(RecaptchaValidator));
            _logger  = logger;
        }

        public async Task<bool> TryValidateAsync(string token, CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            if (options.SecretKey == null || token == null)
                return false;

            var success = true;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "secret", options.SecretKey },
                { "response", token }
            });

            var response = await _http.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, cancellationToken);

            success &= response.StatusCode == HttpStatusCode.OK;

            dynamic result = JObject.Parse(await response.Content.ReadAsStringAsync());

            success &= (bool) result.success;

            if (!success)
            {
                _logger.LogDebug($"Failed reCAPTCHA verification for token '{token}'. {response.ReasonPhrase}");
                return false;
            }

            return true;
        }
    }
}