using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Controllers.OAuth;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;

namespace nhitomi.Controllers
{
    public interface IOAuthHandler
    {
        string AuthorizeUrl { get; }
        string RedirectUrl { get; }

        Task<DbUser> GetOrCreateUserAsync(string code, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Contains endpoints for authenticating users and managing user information.
    /// </summary>
    [Route("users")]
    public class UserController : nhitomiControllerBase
    {
        readonly IServiceProvider _services;
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;
        readonly IUserService _users;
        readonly ICollectionService _collections;

        public UserController(IServiceProvider services, IAuthService auth, IDiscordOAuthHandler discord, IUserService users, ICollectionService collections)
        {
            _services    = services;
            _auth        = auth;
            _discord     = discord;
            _users       = users;
            _collections = collections;
        }

        public class AuthenticateDiscordRequest
        {
            /// <summary>
            /// OAuth code.
            /// </summary>
            [Required]
            public string Code { get; set; }
        }

        public class AuthenticateResponse
        {
            /// <summary>
            /// JWT bearer token.
            /// </summary>
            [Required]
            public string Token { get; set; }

            /// <summary>
            /// Authenticated user information.
            /// </summary>
            [Required]
            public User User { get; set; }
        }

        /// <summary>
        /// Authenticates using Discord OAuth.
        /// </summary>
        /// <param name="request">OAuth data.</param>
        [HttpPost("oauth/discord", Name = "authenticateUserDiscord"), AllowAnonymous, RequireDbWrite]
        public async Task<ActionResult<AuthenticateResponse>> AuthDiscordAsync(AuthenticateDiscordRequest request)
        {
            var user = User = await _discord.GetOrCreateUserAsync(request.Code);

            return new AuthenticateResponse
            {
                Token = await _auth.GenerateTokenAsync(user),
                User  = ProcessUser(user.Convert(_services))
            };
        }

        /// <summary>
        /// Retrieves user information.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}", Name = "getUser")]
        public async Task<ActionResult<User>> GetAsync(string id)
        {
            var result = await _users.GetAsync(id);

            if (!result.TryPickT0(out var user, out _))
                return ResultUtilities.NotFound(id);

            return ProcessUser(user.Convert(_services));
        }

        /// <summary>
        /// Retrieves user information of the requester.
        /// </summary>
        [HttpGet("me", Name = "getSelfUser"), RequireUser]
        public User GetSelfAsync() => ProcessUser(User.Convert(_services));

        /// <summary>
        /// Updates user information.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="model">New user information.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpPut("{id}", Name = "updateUser"), RequireUser(Unrestricted = false), RequireDbWrite]
        public async Task<ActionResult<User>> UpdateAsync(string id, UserBase model, [FromQuery] string reason = null)
        {
            if (UserId != id && !User.HasPermissions(UserPermissions.ManageUsers))
                return ResultUtilities.Forbidden("Insufficient permissions to update this user.");

            var result = await _users.UpdateAsync(id, model, new SnapshotArgs
            {
                Committer = User,
                Event     = SnapshotEvent.AfterModification,
                Reason    = reason,
                Source    = SnapshotSource.User
            });

            if (!result.TryPickT0(out var user, out _))
                return ResultUtilities.NotFound(id);

            return ProcessUser(user.Convert(_services));
        }

        public class RestrictUserRequest
        {
            /// <summary>
            /// Duration of the restriction. Null implies indefinite restriction.
            /// </summary>
            public TimeSpan? Duration { get; set; }
        }

        /// <summary>
        /// Adds a restriction to a user.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.RestrictUsers"/> permission.
        /// </remarks>
        /// <param name="id">User ID.</param>
        /// <param name="request">Restriction request.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpPost("{id}/restrictions", Name = "restrictUser"), RequireUser(Unrestricted = true, Permissions = UserPermissions.RestrictUsers), RequireReason, RequireDbWrite]
        public async Task<ActionResult<User>> RestrictAsync(string id, RestrictUserRequest request, [FromQuery] string reason = null)
        {
            var result = await _users.RestrictAsync(id, UserId, request.Duration, new SnapshotArgs
            {
                Committer = User,
                Event     = SnapshotEvent.AfterModification,
                Reason    = reason,
                Source    = SnapshotSource.User
            });

            if (!result.TryPickT0(out var user, out _))
                return ResultUtilities.NotFound(id);

            return ProcessUser(user.Convert(_services));
        }

        /// <summary>
        /// Ends all currently active restrictions for a user.
        /// </summary>
        /// <remarks>
        /// Requires <see cref="UserPermissions.RestrictUsers"/> permission.
        /// </remarks>
        /// <param name="id">User ID.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpDelete("{id}/restrictions", Name = "unrestrictUser"), RequireUser(Unrestricted = true, Permissions = UserPermissions.RestrictUsers), RequireReason, RequireDbWrite]
        public async Task<ActionResult<User>> UnrestrictAsync(string id, [FromQuery] string reason = null)
        {
            var result = await _users.UnrestrictAsync(id, new SnapshotArgs
            {
                Committer = User,
                Event     = SnapshotEvent.AfterModification,
                Reason    = reason,
                Source    = SnapshotSource.User
            });

            if (!result.TryPickT0(out var user, out _))
                return ResultUtilities.NotFound(id);

            return ProcessUser(user.Convert(_services));
        }

        /// <summary>
        /// Retrieves all collections owned by a user.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}/collections", Name = "getUserCollections"), RequireUser]
        public async Task<ActionResult<SearchResult<Collection>>> GetCollectionsAsync(string id)
        {
            if (UserId != id && !User.HasPermissions(UserPermissions.ManageUsers))
                return ResultUtilities.Forbidden("Insufficient permissions to see user collections.");

            var result = await _collections.GetUserCollectionsAsync(id);

            return result.Project(c => c.Convert(_services));
        }

        /// <summary>
        /// Retrieves a special collection owned by a user.
        /// </summary>
        /// <remarks>
        /// If the user does not own a special collection yet, a new one will be automatically created.
        /// </remarks>
        /// <param name="id">User ID.</param>
        /// <param name="type">Collection object type.</param>
        /// <param name="collection">Special collection type.</param>
        [HttpGet("{id}/collections/{type}/{collection}", Name = "getUserSpecialCollection"), RequireUser, RequireDbWrite]
        public async Task<ActionResult<Collection>> GetSpecialCollectionAsync(string id, ObjectType type, SpecialCollection collection)
        {
            if (UserId != id && !User.HasPermissions(UserPermissions.ManageUsers))
                return ResultUtilities.Forbidden("Insufficient permissions to see user collections.");

            var result = await _collections.GetOrCreateUserSpecialCollectionAsync(id, type, collection);

            if (!result.TryPickT0(out var coll, out _))
                return ResultUtilities.NotFound(id);

            return coll.Convert(_services);
        }
    }
}