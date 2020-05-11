using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public class SuggestQuery
    {
        /// <summary>
        /// String to match as the prefix of suggestions.
        /// </summary>
        [Required]
        public string Prefix { get; set; }

        /// <summary>
        /// Number of suggestions to return.
        /// </summary>
        /// <remarks>
        /// There is a hard limit of 50 items.
        /// </remarks>
        [Required, Range(0, 50)]
        public int Limit { get; set; }

        /// <summary>
        /// True to enable levenshtein-based fuzzy match.
        /// </summary>
        public bool Fuzzy { get; set; }
    }
}