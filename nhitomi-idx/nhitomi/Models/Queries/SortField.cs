using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Queries
{
    public struct SortField<TSort>
    {
        /// <summary>
        /// Object sorting field.
        /// </summary>
        [Required]
        public TSort Value { get; set; }

        /// <summary>
        /// Sorting direction.
        /// </summary>
        [Required]
        public SortDirection Direction { get; set; }

        public static implicit operator SortField<TSort>(TSort sort) => new SortField<TSort>
        {
            Value = sort
        };

        public static implicit operator SortField<TSort>((TSort sort, SortDirection direction) x) => new SortField<TSort>
        {
            Value     = x.sort,
            Direction = x.direction
        };
    }
}