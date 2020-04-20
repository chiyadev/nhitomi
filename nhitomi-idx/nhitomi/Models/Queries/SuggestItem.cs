using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public sealed class SuggestItem
    {
        /// <summary>
        /// ID of the object related to this suggestion.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Suggested completion text.
        /// </summary>
        [Required]
        public string Text { get; set; }

        /// <summary>
        /// Suggestion score.
        /// </summary>
        [Required]
        public double Score { get; set; }
    }
}