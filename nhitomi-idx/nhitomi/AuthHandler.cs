using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using Prometheus;

namespace nhitomi
{
    public class AuthOptions : AuthenticationSchemeOptions { }

    public class AuthHandler : AuthenticationHandler<AuthOptions>
    {
        public static readonly object PayloadItemKey = new object();

        public const string SchemeName = "Bearer";

        readonly IAuthService _auth;

        public AuthHandler(IOptionsMonitor<AuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IAuthService auth)
            : base(options, logger, encoder, clock)
        {
            _auth = auth;
        }

        static readonly Counter _results = Metrics.CreateCounter("server_authentication_results", "Authentication results of requests.", new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

        static readonly Histogram _time = Metrics.CreateHistogram("server_authentication_time", "Time spent on validating authenticated requests.", new HistogramConfiguration
        {
            Buckets = HistogramEx.ExponentialBuckets(0.01, 50, 10)
        });

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // get header
                if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authorization) || authorization.Scheme != Scheme.Name)
                {
                    _results.Labels("none").Inc();
                    return AuthenticateResult.NoResult();
                }

                OneOf<AuthTokenPayload, TokenValidationError> result;

                using (_time.Measure())
                    result = await _auth.ValidateTokenAsync(authorization.Parameter);

                if (!result.TryPickT0(out var payload, out var error))
                {
                    _results.Labels("fail").Inc();
                    return AuthenticateResult.Fail(error.ToString());
                }

                // pass payload down the pipeline
                Context.Items[PayloadItemKey] = payload;

                _results.Labels("success").Inc();
                return AuthenticateResult.Success(_successTicket);
            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail($"Authentication failed. {e.Message}");
            }
        }

        // we don't use identities
        static readonly AuthenticationTicket _successTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(null, SchemeName)), SchemeName);
    }
}