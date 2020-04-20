using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    public class Vote : VoteBase, INanokaObject
    {
        /// <summary>
        /// Vote ID.
        /// </summary>
        [Required, NanokaId]
        public string Id { get; set; }

        /// <summary>
        /// ID of the user that voted.
        /// </summary>
        [Required, NanokaId]
        public string UserId { get; set; }

        /// <summary>
        /// Type of the target object of this vote.
        /// </summary>
        [Required]
        public SnapshotTarget Target { get; set; }

        /// <summary>
        /// ID of the target object of this vote.
        /// </summary>
        [Required, NanokaId]
        public string TargetId { get; set; }

        public override string ToString() => $"{Type} {Target} {TargetId}";
    }

    public class VoteBase
    {
        /// <summary>
        /// Vote type.
        /// </summary>
        [Required]
        public VoteType Type { get; set; }
    }
}