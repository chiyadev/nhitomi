using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Database;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    [ApiController, Authorize]
    public class nhitomiControllerBase : ControllerBase
    {
        AuthTokenPayload CurrentToken => HttpContext != null && HttpContext.Items.TryGetValue(AuthHandler.PayloadItemKey, out var v) ? v as AuthTokenPayload : default;
        DbUser CurrentUser => HttpContext != null && HttpContext.Items.TryGetValue(RequireUserAttribute.UserItemKey, out var v) ? v as DbUser : default;

        string _userId;

        /// <summary>
        /// Gets the currently authenticated user ID, or null if unauthenticated.
        /// Setter is provided for unit testing purposes.
        /// </summary>
        public string UserId
        {
            get => _userId ?? CurrentToken?.UserId ?? User?.Id;
            set => _userId = value;
        }

        DbUser _user;

        /// <summary>
        /// Gets the currently authenticated user information, or null if unauthenticated or <see cref="RequireUserAttribute"/> did not take effect.
        /// Setter is provided for unit testing purposes.
        /// </summary>
        public new DbUser User
        {
            get => _user ?? CurrentUser;
            set => _user = value;
        }

        /// <inheritdoc cref="DbUser.HasPermissions"/>
        protected bool HasPermissions(UserPermissions permissions) => User != null && User.HasPermissions(permissions);

        /// <summary>
        /// Process user information and erases confidential fields with respect to the requester's permissions.
        /// </summary>
        protected User ProcessUser(User user)
        {
            // self-only
            if (user.Id != UserId)
                user.DiscordConnection = null;

            // manage users
            if (!HasPermissions(UserPermissions.ManageUsers))
                user.Email = null;

            return user;
        }
    }
}