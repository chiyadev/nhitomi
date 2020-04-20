using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    public class UserRestriction
    {
        /// <summary>
        /// Time when this restriction started.
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Time when this restriction ended.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// ID of the user that made this restriction, or null if it was automated by the system.
        /// </summary>
        [NanokaId]
        public string ModeratorId { get; set; }

        /// <summary>
        /// Reason describing why this restriction was made.
        /// </summary>
        [Required]
        public string Reason { get; set; }
    }
}