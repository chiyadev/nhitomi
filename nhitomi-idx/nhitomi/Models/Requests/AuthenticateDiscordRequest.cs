using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class AuthenticateDiscordRequest
    {
        /// <summary>
        /// OAuth code.
        /// </summary>
        [Required]
        public string Code { get; set; }
    }
}