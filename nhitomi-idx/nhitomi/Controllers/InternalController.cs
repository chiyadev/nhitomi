using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints used internally by nhitomi.
    /// </summary>
    /// <remarks>
    /// Regular users should ignore these endpoints because they will not be able to access them.
    /// </remarks>
    [Route("internal")]
    public class InternalController : nhitomiControllerBase
    {
        readonly IConfiguration _config;
        readonly ServerOptions _serverOptions;
        readonly IElasticClient _elastic;
        readonly IUserService _users;
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;

        public InternalController(IConfiguration config, IOptionsSnapshot<ServerOptions> serverOptions, IElasticClient elastic, IUserService users, IAuthService auth, IDiscordOAuthHandler discord)
        {
            _config        = config;
            _serverOptions = serverOptions.Value;
            _elastic       = elastic;
            _users         = users;
            _auth          = auth;
            _discord       = discord;
        }

        /// <summary>
        /// Retrieves internal server configuration values.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.ManageServer"/> permission.
        /// </remarks>
        [HttpGet("config", Name = "getServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public Dictionary<string, string> GetConfig()
            => _config.Get<Dictionary<string, string>>();

        /// <summary>
        /// Updates internal server configuration values.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.ManageServer"/> permission.
        /// Changes may not take effect immediately. Some changes will require a full server restart.
        /// </remarks>
        /// <param name="data">New configuration object to replace with.</param>
        [HttpPut("config", Name = "setServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public async Task<Dictionary<string, string>> SetConfigAsync(Dictionary<string, string> data)
        {
            var entry = _elastic.Entry<DbCompositeConfig>(DbCompositeConfig.DefaultId);

            entry.Value ??= new DbCompositeConfig();

            entry.Value.Config = data;

            await entry.UpdateAsync();
            await Task.Delay(_serverOptions.ConfigurationReloadInterval);

            return GetConfig();
        }

        /// <summary>
        /// Authenticates a user bypassing OAuth2.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.CreateUsers"/> permission.
        /// </remarks>
        /// <param name="id">User ID.</param>
        [HttpGet("auth/direct/{id}", Name = "authenticateUserDirect"), RequireUser(Permissions = UserPermissions.CreateUsers)]
        public async Task<ActionResult<UserController.AuthenticateResponse>> AuthDirectAsync(string id)
        {
            var result = await _users.GetAsync(id);

            if (!result.TryPickT0(out var user, out _))
                return ResultUtilities.NotFound(id);

            return new UserController.AuthenticateResponse
            {
                Token = await _auth.GenerateTokenAsync(user),
                User  = ProcessUser(user.Convert())
            };
        }

        public class GetOrCreateDiscordUserRequest
        {
            /// <remarks>
            /// This is a string but should represent a 64-bit unsigned integer.
            /// </remarks>
            [Required]
            public string Id { get; set; }

            [Required]
            public string Username { get; set; }

            [Required]
            public int Discriminator { get; set; }

            public string Locale { get; set; }
            public string Email { get; set; }
        }

        /// <summary>
        /// Gets or creates a user directly using Discord OAuth2 connection information.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.CreateUsers"/> permission.
        /// </remarks>
        /// <param name="request">Discord connection information.</param>
        [HttpPost("auth/discord", Name = "getOrCreateUserDiscord"), RequireUser(Permissions = UserPermissions.CreateUsers)]
        public async Task<UserController.AuthenticateResponse> GetOrCreateDiscordUserAsync(GetOrCreateDiscordUserRequest request)
        {
            var user = await _discord.GetOrCreateUserAsync(new DiscordOAuthUser
            {
                Id            = ulong.Parse(request.Id),
                Username      = request.Username,
                Discriminator = request.Discriminator,

                // optional
                Locale = request.Locale,
                Email  = request.Email
            });

            return new UserController.AuthenticateResponse
            {
                Token = await _auth.GenerateTokenAsync(user),
                User  = ProcessUser(user.Convert())
            };
        }
    }
}