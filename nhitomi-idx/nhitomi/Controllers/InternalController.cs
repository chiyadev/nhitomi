using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nhitomi.Controllers.OAuth;
using nhitomi.Database;
using nhitomi.Models;
using Stripe;
using Stripe.Checkout;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints used internally by nhitomi.
    /// </summary>
    /// <remarks>
    /// Regular users should ignore these endpoints because they will not be able to access them.
    /// Attempts to access these endpoints unauthorized may result in a restriction or ban.
    /// </remarks>
    [Route("internal")]
    public class InternalController : nhitomiControllerBase
    {
        readonly IServiceProvider _services;
        readonly IDynamicOptions _options;
        readonly IUserService _users;
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;
        readonly IStripeService _stripe;
        readonly IOptionsMonitor<StripeServiceOptions> _stripeOptions;

        public InternalController(IServiceProvider services, IDynamicOptions options, IUserService users, IAuthService auth, IDiscordOAuthHandler discord, IStripeService stripe, IOptionsMonitor<StripeServiceOptions> stripeOptions)
        {
            _services      = services;
            _options       = options;
            _users         = users;
            _auth          = auth;
            _discord       = discord;
            _stripe        = stripe;
            _stripeOptions = stripeOptions;
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
        [HttpPost("auth/discord", Name = "getOrCreateUserDiscord"), RequireUser(Permissions = UserPermissions.CreateUsers)] // no RequireDbWrite to avoid breaking nhitomi-discord
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
                User  = ProcessUser(user.Convert(_services))
            };
        }

        public class DonationProgressRequest
        {
            /// <summary>
            /// Donation amount in USD.
            /// </summary>
            [Required]
            public double Amount { get; set; }
        }

        /// <summary>
        /// Adds progress to the donation goal for the current month.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.ManageServer"/> permission.
        /// </remarks>
        /// <param name="request">Donation request.</param>
        [HttpPost("donations", Name = "addDonationProgress"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public async Task<ActionResult<DonationInfo>> AddDonationProgress(DonationProgressRequest request)
        {
            var result = await _stripe.AddDonationProgress(DateTime.UtcNow, request.Amount);

            return result.Convert(_services);
        }

        /// <summary>
        /// Defines the Webhook callback endpoint for Stripe.
        /// </summary>
        /// <returns></returns>
        [HttpPost("stripe"), AllowAnonymous, ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> StripeCallbackAsync()
        {
            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(),
                    Request.Headers["Stripe-Signature"],
                    _stripeOptions.CurrentValue.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException)
            {
                // event construction failed, so request wasn't valid
                return ResultUtilities.BadRequest("You're not stripe, are you?");
            }

            switch (stripeEvent.Type)
            {
                case Events.CheckoutSessionCompleted when stripeEvent.Data.Object is Session session:
                    await _stripe.HandleSupporterCheckoutCompletedAsync(session);
                    break;

                default:
                    return ResultUtilities.BadRequest($"Stripe event {stripeEvent.Type} should not be received by this server.");
            }

            return Ok();
        }
    }
}