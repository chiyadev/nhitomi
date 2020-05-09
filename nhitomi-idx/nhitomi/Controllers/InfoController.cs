using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models.Requests;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints for retrieving API information.
    /// </summary>
    [Route("/")]
    public class InfoController : nhitomiControllerBase
    {
        readonly IDiscordOAuthHandler _discordOAuth;
        readonly RecaptchaOptions _recaptchaOptions;

        public InfoController(IOptionsSnapshot<RecaptchaOptions> recaptchaOptions, IDiscordOAuthHandler discordOAuth)
        {
            _discordOAuth     = discordOAuth;
            _recaptchaOptions = recaptchaOptions.Value;
        }

        [HttpGet, AllowAnonymous, ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult Get() => Redirect($"{Startup.ApiBasePath}/index.html"); // redirect to docs

        /// <summary>
        /// Retrieves unauthenticated API information.
        /// </summary>
        [HttpGet("info", Name = "getInfo"), AllowAnonymous]
        public GetInfoResponse GetInfo() => new GetInfoResponse
        {
            Version          = VersionInfo.Commit,
            RecaptchaSiteKey = _recaptchaOptions.SiteKey,
            DiscordOAuthUrl  = _discordOAuth.AuthorizeUrl
        };

        /// <summary>
        /// Retrieves authenticated API information.
        /// </summary>
        [HttpGet("info/auth", Name = "getInfoAuthenticated"), RequireUser]
        public GetInfoAuthenticatedResponse GetInfoAuthenticated() => new GetInfoAuthenticatedResponse
        {
            Version          = VersionInfo.Commit,
            RecaptchaSiteKey = _recaptchaOptions.SiteKey,
            DiscordOAuthUrl  = _discordOAuth.AuthorizeUrl,

            User = User.Convert()
        };
    }
}