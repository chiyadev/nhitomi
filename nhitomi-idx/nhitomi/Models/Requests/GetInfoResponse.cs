using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class GetInfoResponse
    {
        /// <summary>
        /// Backend version information.
        /// </summary>
        [Required]
        public GitCommit Version { get; set; }

        /// <summary>
        /// reCAPTCHA site key to use to obtain tokens for authorizing certain endpoints.
        /// If null, recaptcha is not required at all.
        /// </summary>
        public string RecaptchaSiteKey { get; set; }

        /// <summary>
        /// Discord OAuth authorization URL.
        /// </summary>
        [Required]
        public string DiscordOAuthUrl { get; set; }
    }
}