using System;
using System.ComponentModel.DataAnnotations;
using Force.DeepCloner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nhitomi.Controllers.OAuth;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Scrapers;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints for retrieving API information.
    /// </summary>
    [Route("/")]
    public class InfoController : nhitomiControllerBase
    {
        readonly IServiceProvider _services;
        readonly ILinkGenerator _link;
        readonly IDiscordOAuthHandler _discordOAuth;
        readonly IScraperService _scrapers;
        readonly RecaptchaOptions _recaptchaOptions;

        public InfoController(IServiceProvider services, ILinkGenerator link, IOptionsSnapshot<RecaptchaOptions> recaptchaOptions, IDiscordOAuthHandler discordOAuth, IScraperService scrapers)
        {
            _services         = services;
            _link             = link;
            _discordOAuth     = discordOAuth;
            _scrapers         = scrapers;
            _recaptchaOptions = recaptchaOptions.Value;
        }

        [HttpGet, AllowAnonymous, ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult Get() => Redirect($"{Startup.ApiBasePath}/index.html"); // redirect to docs

        public class GetInfoResponse
        {
            /// <summary>
            /// Public frontend URL.
            /// </summary>
            [Required]
            public string PublicUrl { get; set; }

            /// <summary>
            /// Backend version information.
            /// </summary>
            [Required]
            public GitCommit Version { get; set; }

            /// <summary>
            /// reCAPTCHA site key to use to obtain tokens for authorizing certain endpoints.
            /// If null, recaptcha is not required at all.
            /// </summary>
            public string RecaptchaSiteKey { get; set; }

            /// <summary>
            /// Discord OAuth authorization URL.
            /// </summary>
            [Required]
            public string DiscordOAuthUrl { get; set; }

            /// <summary>
            /// List of supported scrapers.
            /// </summary>
            [Required]
            public ScraperInfo[] Scrapers { get; set; }
        }

        /// <summary>
        /// Retrieves unauthenticated API information.
        /// </summary>
        [HttpGet("info", Name = "getInfo"), AllowAnonymous]
        public GetInfoResponse GetInfo() => new GetInfoResponse
        {
            PublicUrl        = _link.GetWebLink("/"),
            Version          = VersionInfo.Commit,
            RecaptchaSiteKey = _recaptchaOptions.SiteKey,
            DiscordOAuthUrl  = _discordOAuth.AuthorizeUrl,
            Scrapers = _scrapers.ToArray(s => new ScraperInfo
            {
                Name               = s.Name,
                Type               = s.Type,
                Category           = s.Category,
                Enabled            = s.Enabled,
                Url                = s.Url,
                GalleryRegexLax    = s.UrlRegex?.Lax.ToString(),
                GalleryRegexStrict = s.UrlRegex?.Strict.ToString()
            })
        };

        public class GetInfoAuthenticatedResponse : GetInfoResponse
        {
            /// <summary>
            /// Currently authenticated user information, or null if not authenticated.
            /// </summary>
            [Required]
            public User User { get; set; }
        }

        /// <summary>
        /// Retrieves authenticated API information.
        /// </summary>
        [HttpGet("info/auth", Name = "getInfoAuthenticated"), RequireUser]
        public GetInfoAuthenticatedResponse GetInfoAuthenticated() => GetInfo().ShallowCloneTo(new GetInfoAuthenticatedResponse
        {
            User = ProcessUser(User.Convert(_services))
        });
    }
}