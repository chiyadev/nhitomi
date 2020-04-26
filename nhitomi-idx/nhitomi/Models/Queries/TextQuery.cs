using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;

namespace nhitomi.Models.Queries
{
    public class TextQuery
    {
        /// <summary>
        /// Values to match.
        /// </summary>
        [Required, MaxLength(16)]
        public string[] Values { get; set; }

        /// <summary>
        /// Match mode.
        /// </summary>
        public QueryMatchMode Mode { get; set; }

        [JsonIgnore]
        public bool IsSpecified => Values != null && Values.Any(v => !string.IsNullOrEmpty(v));

        public static implicit operator TextQuery(string value) => new[] { value };

        public static implicit operator TextQuery(string[] values) => new TextQuery
        {
            Values = values
        };

        public static implicit operator TextQuery(List<string> values) => values?.ToArray();
    }
}