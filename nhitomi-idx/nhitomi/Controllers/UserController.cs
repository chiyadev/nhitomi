using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Models.Requests;

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
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;
        readonly IUserService _users;
        readonly ICollectionService _collections;

        public UserController(IAuthService auth, IDiscordOAuthHandler discord, IUserService users, ICollectionService collections)
        {
            _auth        = auth;
            _discord     = discord;
            _users       = users;
            _collections = collections;
        }

        /// <summary>
        /// Authenticates using Discord OAuth.
        /// </summary>
        /// <param name="request">OAuth data.</param>
        [HttpPost("oauth/discord", Name = "authenticateUserDiscord"), AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> AuthDiscordAsync(AuthenticateDiscordRequest request)
        {
            var user = await _discord.GetOrCreateUserAsync(request.Code);

            return new AuthenticateResponse
            {
                Token = await _auth.GenerateTokenAsync(user),
                User  = user.Convert() // no ProcessUser
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

            return ProcessUser(user.Convert());
        }

        /// <summary>
        /// Retrieves user information of the requester.
        /// </summary>
        [HttpGet("me", Name = "getSelfUser"), RequireUser]
        public User GetSelfAsync() => ProcessUser(User.Convert());

        /// <summary>
        /// Updates user information.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="model">New user information.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpPut("{id}", Name = "updateUser"), RequireUser(Unrestricted = false)]
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

            return ProcessUser(user.Convert());
        }

        /// <summary>
        /// Adds a restriction to a user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="request">Restriction request.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpPost("{id}/restrictions", Name = "restrictUser"), RequireUser(Unrestricted = true, Permissions = UserPermissions.RestrictUsers), RequireReason]
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

            return ProcessUser(user.Convert());
        }

        /// <summary>
        /// Ends all currently active restrictions for a user.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <param name="reason">Reason for this action.</param>
        [HttpDelete("{id}/restrictions", Name = "unrestrictUser"), RequireUser(Unrestricted = true, Permissions = UserPermissions.RestrictUsers), RequireReason]
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

            return ProcessUser(user.Convert());
        }

        /// <summary>
        /// Retrieves all collections owned by a user.
        /// </summary>
        /// <param name="id">User ID.</param>
        [HttpGet("{id}/collections", Name = "getUserCollections"), RequireUser]
        public async Task<ActionResult<SearchResult<Collection>>> GetCollectionsAsync(string id)
        {
            var publicOnly = UserId != id && !User.HasPermissions(UserPermissions.ManageUsers);

            var result = await _collections.GetUserCollectionsAsync(id, publicOnly);

            return result.Project(c => c.Convert());
        }
    }
}