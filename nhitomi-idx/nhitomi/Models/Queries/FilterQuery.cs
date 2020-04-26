using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;

namespace nhitomi.Models.Queries
{
    public class FilterQuery<T>
    {
        /// <summary>
        /// Values to match.
        /// </summary>
        [Required, MaxLength(16)]
        public T[] Values { get; set; }

        /// <summary>
        /// Match mode.
        /// </summary>
        public QueryMatchMode Mode { get; set; }

        [JsonIgnore]
        public bool IsSpecified => Values != null && Values.Length != 0;

        public FilterQuery<T2> Project<T2>(Func<T, T2> projection) => new FilterQuery<T2>
        {
            Values = Values?.Select(projection).ToArray(),
            Mode   = Mode
        };

        public static implicit operator FilterQuery<T>(T value) => new[] { value };

        public static implicit operator FilterQuery<T>(T[] values) => new FilterQuery<T>
        {
            Values = values
        };

        public static implicit operator FilterQuery<T>(List<T> values) => values?.ToArray();
    }
}