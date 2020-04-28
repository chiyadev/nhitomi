using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Requests;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints for retrieving API information and various configurations.
    /// </summary>
    [Route("/")]
    public class InfoController : nhitomiControllerBase
    {
        readonly IConfiguration _config;
        readonly IDiscordOAuthHandler _discordOAuth;
        readonly ServerOptions _serverOptions;
        readonly RecaptchaOptions _recaptchaOptions;
        readonly IElasticClient _elastic;

        public InfoController(IConfiguration config, IOptionsSnapshot<ServerOptions> serverOptions, IOptionsSnapshot<RecaptchaOptions> recaptchaOptions, IDiscordOAuthHandler discordOAuth, IElasticClient elastic)
        {
            _config           = config;
            _discordOAuth     = discordOAuth;
            _serverOptions    = serverOptions.Value;
            _recaptchaOptions = recaptchaOptions.Value;
            _elastic          = elastic;
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
        [HttpGet("info/authed", Name = "getInfoAuthenticated"), RequireUser]
        public GetInfoAuthenticatedResponse GetInfoAuthenticated() => new GetInfoAuthenticatedResponse
        {
            Version          = VersionInfo.Commit,
            RecaptchaSiteKey = _recaptchaOptions.SiteKey,
            DiscordOAuthUrl  = _discordOAuth.AuthorizeUrl,

            User = User.Convert()
        };

        /// <summary>
        /// Retrieves internal server configuration values.
        /// </summary>
        /// <remarks>
        /// This requires <see cref="UserPermissions.ManageServer"/> permission.
        /// </remarks>
        [HttpGet("internal/config", Name = "getServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public Dictionary<string, string> GetServerConfig()
            => _config.Get<Dictionary<string, string>>()
                      .ToDictionary(
                           x => x.Key.Replace(":", "."),
                           x => x.Value);

        /// <summary>
        /// Updates internal server configuration values.
        /// </summary>
        /// <remarks>
        /// This requires <see cref="UserPermissions.ManageServer"/> permission.
        /// Changes may not take effect immediately. Some changes will require a full server restart.
        /// </remarks>
        /// <param name="data">New configuration object to replace with.</param>
        [HttpPut("internal/config", Name = "setServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public async Task<Dictionary<string, string>> SetServerConfig(Dictionary<string, string> data)
        {
            data = data.ToDictionary(
                x => x.Key.Replace(".", ":"),
                x => x.Value);

            var entry = _elastic.Entry<DbCompositeConfig>(DbCompositeConfig.DefaultId);

            entry.Value ??= new DbCompositeConfig();

            entry.Value.Config = data;

            await entry.UpdateAsync();
            await Task.Delay(_serverOptions.ConfigurationReloadInterval);

            return GetServerConfig();
        }
    }
}