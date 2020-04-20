using System.Collections.Generic;
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
        /// If null, recaptcha is not required for those endpoints.
        /// </summary>
        public string RecaptchaSiteKey { get; set; }

        /// <summary>
        /// Number of objects in the database.
        /// </summary>
        [Required]
        public Dictionary<ObjectType, int> Counters { get; set; }

        /// <summary>
        /// Currently authenticated user information, or null if not authenticated.
        /// </summary>
        public User User { get; set; }
    }
}