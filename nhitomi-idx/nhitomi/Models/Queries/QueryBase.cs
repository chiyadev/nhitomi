using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public abstract class QueryBase
    {
        /// <summary>
        /// Number of items to skip before returning.
        /// </summary>
        /// <remarks>
        /// There is a hard limit of 950 items (1000 items in total retrievable via search).
        /// </remarks>
        [Range(0, 950)]
        public int Offset { get; set; }

        /// <summary>
        /// Number of items to return before stopping.
        /// </summary>
        /// <remarks>
        /// There is a hard limit of 50 items.
        /// </remarks>
        [Required, Range(0, 50)]
        public int Limit { get; set; }

        /// <summary>
        /// Match mode fot the root query. This is <see cref="QueryMatchMode.All"/> by default.
        /// </summary>
        public QueryMatchMode Mode { get; set; } = QueryMatchMode.All;
    }

    public abstract class QueryBase<TSort> : QueryBase where TSort : Enum
    {
        /// <summary>
        /// Sorting to apply when returning results.
        /// </summary>
        [Required]
        public List<SortField<TSort>> Sorting { get; set; } = new List<SortField<TSort>>();
    }
}