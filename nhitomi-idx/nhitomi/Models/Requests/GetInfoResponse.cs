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
        public Dictionary<SnapshotTarget, int> Counters { get; set; }

        /// <summary>
        /// Number of users currently connected to.
        /// </summary>
        [Required]
        public int ConnectedUsers { get; set; }
    }
}