using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    public class Vote : VoteBase, IHasId
    {
        /// <summary>
        /// Vote ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// ID of the user that voted.
        /// </summary>
        [Required, nhitomiId]
        public string UserId { get; set; }

        /// <summary>
        /// Type of the target object of this vote.
        /// </summary>
        [Required]
        public ObjectType Target { get; set; }

        /// <summary>
        /// ID of the target object of this vote.
        /// </summary>
        [Required, nhitomiId]
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

    public enum VoteType
    {
        /// <summary>
        /// Vote is positive.
        /// </summary>
        Up = 0,

        /// <summary>
        /// Vote is negative.
        /// </summary>
        Down = 1
    }
}