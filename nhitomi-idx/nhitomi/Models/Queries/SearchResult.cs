using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public  class SearchResult<T>
    {
        /// <summary>
        /// How long it took for the search to be completed.
        /// </summary>
        [Required]
        public TimeSpan Took { get; set; }

        /// <summary>
        /// Total number of matched items.
        /// </summary>
        [Required]
        public int Total { get; set; }

        /// <summary>
        /// Search result items.
        /// </summary>
        [Required]
        public T[] Items { get; set; }

        public SearchResult<T2> Project<T2>(Func<T, T2> projection) => new SearchResult<T2>
        {
            Took  = Took,
            Total = Total,
            Items = Items?.ToArray(projection)
        };
    }
}