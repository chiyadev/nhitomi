using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public abstract class SuggestResult
    {
        /// <summary>
        /// How long it took for the query to be completed.
        /// </summary>
        [Required]
        public TimeSpan Took { get; set; }
    }
}