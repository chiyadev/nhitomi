using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public sealed class SuggestQuery
    {
        /// <summary>
        /// String to match as the prefix of suggestions.
        /// </summary>
        [Required]
        public string Prefix { get; set; }

        /// <summary>
        /// Number of suggestions to return.
        /// </summary>
        [Required, Range(0, int.MaxValue)]
        public int Limit { get; set; }

        /// <summary>
        /// True to enable levenshtein-based fuzzy match.
        /// </summary>
        public bool Fuzzy { get; set; }
    }
}