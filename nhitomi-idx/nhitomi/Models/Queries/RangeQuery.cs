using System;
using Newtonsoft.Json;

namespace nhitomi.Models.Queries
{
    /// <remarks>
    /// Ranges are inclusive by default.
    /// </remarks>
    public class RangeQuery<T> where T : struct
    {
        /// <summary>
        /// Minimum value to match.
        /// </summary>
        public T? Minimum { get; set; }

        /// <summary>
        /// Maximum value to match.
        /// </summary>
        public T? Maximum { get; set; }

        /// <summary>
        /// Whether the match is inclusive or exclusive.
        /// Match is inclusive by default.
        /// </summary>
        public bool Exclusive { get; set; }

        [JsonIgnore]
        public bool IsSpecified => Minimum != null || Maximum != null;

        public RangeQuery<T2> Project<T2>(Func<T, T2> projection) where T2 : struct => new RangeQuery<T2>
        {
            Minimum   = Minimum == null ? null as T2? : projection(Minimum.Value),
            Maximum   = Maximum == null ? null as T2? : projection(Maximum.Value),
            Exclusive = Exclusive
        };

        public static implicit operator RangeQuery<T>(T value) => (T?) value;
        public static implicit operator RangeQuery<T>(T? value) => (value, value);

        public static implicit operator RangeQuery<T>((T min, T max) x) => ((T?) x.min, (T?) x.max);
        public static implicit operator RangeQuery<T>((T? min, T max) x) => (x.min, (T?) x.max);
        public static implicit operator RangeQuery<T>((T min, T? max) x) => ((T?) x.min, x.max);

        public static implicit operator RangeQuery<T>((T? min, T? max) x) => new RangeQuery<T>
        {
            Minimum = x.min,
            Maximum = x.max
        };
    }
}