using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class NewUserInviteRequest
    {
        /// <summary>
        /// Invite expiry time in number of days.
        /// </summary>
        /// <remarks>
        /// Invites are active for at least 30 minutes and at most 24 hours.
        /// </remarks>
        [Required, Range(30, 60 * 24)]
        public double ExpiryMinutes { get; set; }
    }
}