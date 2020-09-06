using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Controllers.OAuth;
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
        readonly IServiceProvider _services;
        readonly IDynamicOptions _options;
        readonly IUserService _users;
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;

        public InternalController(IServiceProvider services, IDynamicOptions options, IUserService users, IAuthService auth, IDiscordOAuthHandler discord)
        {
            _services = services;
            _options  = options;
            _users    = users;
            _auth     = auth;
            _discord  = discord;
        }

        // use a list with key-value entries to avoid key names getting lowercased when using a dict
        public class ConfigEntry
        {
            /// <summary>
            /// Name of the configuration field.
            /// </summary>
            [Required]
            public string Key { get; set; }

            /// <summary>
            /// Value of the configuration.
            /// </summary>
            [Required]
            public string Value { get; set; }
        }

        /// <summary>
        /// Retrieves internal server configuration values.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.ManageServer"/> permission.
        /// </remarks>
        [HttpGet("config", Name = "getServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public List<ConfigEntry> GetConfig()
        {
            var list = new List<ConfigEntry>();

            foreach (var (key, value) in _options.GetMapping())
                list.Add(new ConfigEntry { Key = key, Value = value });

            return list;
        }

        public class SetConfigRequest
        {
            /// <summary>
            /// Name of the configuration field.
            /// </summary>
            [Required]
            public string Key { get; set; }

            /// <summary>
            /// Value of the configuration, or null to delete the field.
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// Updates internal server configuration value.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.ManageServer"/> permission.
        /// Changes may not take effect immediately. Some changes may require a full server restart.
        /// </remarks>
        /// <param name="request">Set config request.</param>
        [HttpPost("config", Name = "setServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)] // no RequireDbWrite
        public async Task<List<ConfigEntry>> SetConfigAsync(SetConfigRequest request)
        {
            await _options.SetAsync(request.Key, request.Value);

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
                User  = ProcessUser(user.Convert(_services))
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
        [HttpPost("auth/discord", Name = "getOrCreateUserDiscord"), RequireUser(Permissions = UserPermissions.CreateUsers)] // no RequireDbWrite
        public async Task<UserController.AuthenticateResponse> GetOrCreateDiscordUserAsync(GetOrCreateDiscordUserRequest request)
        {
            // RequireDbWrite is missing because it's probably better for some user information to get lost, than to block this route which nhitomi-discord critically depends on

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
                User  = ProcessUser(user.Convert(_services))
            };
        }
    }
}