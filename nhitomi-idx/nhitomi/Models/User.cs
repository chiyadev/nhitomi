using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a user on nhitomi.
    /// </summary>
    /// <remarks>
    /// The following properties may not be returned depending on the requester's permissions:
    /// - <see cref="Email"/>
    /// - <see cref="DiscordConnection"/>
    /// </remarks>
    public class User : UserBase, IHasId, IHasUpdatedTime
    {
        /// <summary>
        /// User ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this user was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this user was updated.
        /// </summary>
        [Required]
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// User username.
        /// Note that there is no username uniqueness guarantee.
        /// </summary>
        [Required, MinLength(UsernameMinLength), MaxLength(UsernameMaxLength)]
        public string Username { get; set; }

        /// <summary>
        /// User email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// List of all restrictions on this user.
        /// </summary>
        [Required]
        public UserRestriction[] Restrictions { get; set; }

        /// <summary>
        /// User permissions.
        /// </summary>
        [Required]
        public UserPermissions[] Permissions { get; set; }

        /// <summary>
        /// Discord connection information.
        /// </summary>
        public UserDiscordConnection DiscordConnection { get; set; }
    }

    public class UserBase
    {
        public const int UsernameMinLength = 4;
        public const int UsernameMaxLength = 20;
    }

    [Flags]
    public enum UserPermissions
    {
        /// <summary>
        /// User has no special permissions.
        /// </summary>
        None = 0,

        /// <summary>
        /// User is an administrator.
        /// This flag effectively enables every other flag.
        /// </summary>
        Administrator = 1,

        /// <summary>
        /// Can access endpoints of other users.
        /// </summary>
        ManageUsers = 1 << 1,

        /// <summary>
        /// Can restrict and derestrict users.
        /// </summary>
        RestrictUsers = 1 << 2,

        /// <summary>
        /// Can manage internal server configuration, maintenance mode, restart, etc.
        /// </summary>
        ManageConfig = 1 << 3
    }
}