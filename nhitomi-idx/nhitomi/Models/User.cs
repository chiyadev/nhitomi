using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Whether this user is an nhitomi supporter.
        /// </summary>
        /// <remarks>
        /// This is a convenience field for checking <see cref="SupporterInfo"/>'s end time.
        /// </remarks>
        [Required]
        public bool IsSupporter => DateTime.UtcNow < SupporterInfo?.EndTime;

        /// <summary>
        /// Supporter information.
        /// </summary>
        public UserSupporterInfo SupporterInfo { get; set; }
    }

    public class UserBase
    {
        public const int UsernameMinLength = 4;
        public const int UsernameMaxLength = 20;

        /// <summary>
        /// User configured language.
        /// </summary>
        [Required]
        public LanguageType Language { get; set; }

        /// <summary>
        /// True to allow sharing collections with this user.
        /// </summary>
        [Required]
        public bool AllowSharedCollections { get; set; }

        /// <summary>
        /// IDs of special collections.
        /// </summary>
        public Dictionary<ObjectType, Dictionary<SpecialCollection, string>> SpecialCollections { get; set; }
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
        /// Can restrict and unrestrict users.
        /// </summary>
        RestrictUsers = 1 << 2,

        /// <summary>
        /// Can manage internal server configuration.
        /// </summary>
        ManageServer = 1 << 3,

        /// <summary>
        /// Can create users bypassing OAuth2 procedures.
        /// </summary>
        CreateUsers = 1 << 4
    }
}