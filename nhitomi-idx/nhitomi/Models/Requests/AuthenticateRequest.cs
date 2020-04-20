using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class AuthenticateRequest
    {
        /// <summary>
        /// User's username.
        /// </summary>
        [Required, MaxLength(UserBase.UsernameMaxLength)]
        public string Username { get; set; }

        /// <summary>
        /// User's password.
        /// </summary>
        [Required, MaxLength(UserBase.PasswordMaxLength)]
        public string Password { get; set; }
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
}