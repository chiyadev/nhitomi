using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class AuthOptions : AuthenticationSchemeOptions { }

    [MessagePackObject]
    public class AuthTokenPayload
    {
        [Key(0)]
        public string UserId { get; set; }

        [Key(1)]
        public DateTime Expiry { get; set; }

        [Key(2)]
        public long SessionId { get; set; }
    }

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

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // get header
                if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authorization) || authorization.Scheme != Scheme.Name)
                    return AuthenticateResult.NoResult();

                var (payload, message) = await _auth.ValidateTokenAsync(authorization.Parameter);

                if (payload == null)
                    return AuthenticateResult.Fail(message);

                // pass payload down the pipeline
                Context.Items[PayloadItemKey] = payload;

                return AuthenticateResult.Success(_readOnlyTicket);
            }
            catch (Exception e)
            {
                return AuthenticateResult.Fail($"Authentication failed: {e.Message}");
            }
        }

        // we don't use identities
        static readonly AuthenticationTicket _readOnlyTicket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(null, SchemeName)), SchemeName);
    }
}