using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    public class UserInvite : INanokaObject, IHasCreatedTime
    {
        /// <summary>
        /// Invite ID.
        /// </summary>
        [Required, NanokaId]
        public string Id { get; set; }

        /// <summary>
        /// True if this invite was accepted by a user.
        /// </summary>
        [Required]
        public bool Accepted { get; set; }

        /// <summary>
        /// ID of the user who created this invite.
        /// </summary>
        [Required, NanokaId]
        public string InviterId { get; set; }

        /// <summary>
        /// ID of the user who accepted this invite and created themselves, or null if not accepted yet.
        /// </summary>
        [NanokaId]
        public string InviteeId { get; set; }

        /// <summary>
        /// Time when this invite was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this invite would expire.
        /// </summary>
        [Required]
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Time when this invite was accepted, or null if not accepted yet.
        /// </summary>
        public DateTime? AcceptedTime { get; set; }
    }
}