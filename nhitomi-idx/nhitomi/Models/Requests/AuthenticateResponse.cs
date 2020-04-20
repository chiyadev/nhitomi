using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
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