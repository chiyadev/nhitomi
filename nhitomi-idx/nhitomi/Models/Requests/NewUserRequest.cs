using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models.Requests
{
    public class NewUserRequest
    {
        /// <summary>
        /// User's username.
        /// </summary>
        [Required, MinLength(UserBase.UsernameMinLength), MaxLength(UserBase.UsernameMaxLength), RegularExpression(UserBase.UsernameRegex)]
        public string Username { get; set; }

        /// <summary>
        /// User's password.
        /// </summary>
        [Required, MinLength(UserBase.PasswordMinLength), MaxLength(UserBase.PasswordMaxLength)]
        public string Password { get; set; }

        /// <summary>
        /// ID of the invite that this new user is accepting.
        /// This can be null for open registrations.
        /// </summary>
        [NanokaId, RequiredInvite]
        public string InviteId { get; set; }
    }
}