using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class GetAcceptableInviteResponse
    {
        /// <summary>
        /// Invite information.
        /// </summary>
        [Required]
        public UserInvite Invite { get; set; }

        /// <summary>
        /// Inviter information.
        /// </summary>
        [Required]
        public User Inviter { get; set; }
    }
}