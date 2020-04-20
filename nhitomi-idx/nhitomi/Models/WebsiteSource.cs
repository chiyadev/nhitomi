using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    public class WebsiteSource : IEquatable<WebsiteSource>
    {
        /// <summary>
        /// Website domain name without scheme or path. e.g. "imgur.com"
        /// </summary>
        [Required, MinLength(5), MaxLength(32)]
        public string Website { get; set; }

        /// <summary>
        /// Identifier used by the website (not necessarily the path). e.g. "cnu2RrM" (implies path "/gallery/cnu2RrM")
        /// </summary>
        [Required, MaxLength(64)]
        public string Identifier { get; set; }

        public override bool Equals(object obj) => obj is WebsiteSource src && Equals(src);

        public bool Equals(WebsiteSource other) => other != null &&
                                                   Website == other.Website &&
                                                   Identifier == other.Identifier;

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                return ((Website != null ? Website.GetHashCode() : 0) * 397) ^ (Identifier != null ? Identifier.GetHashCode() : 0);
                // ReSharper restore NonReadonlyMemberInGetHashCode
            }
        }

        /// <summary>
        /// Parses the format "{website}/{identifier]".
        /// </summary>
        public static WebsiteSource Parse(string combined)
        {
            var parts = combined.Split(new[] { '/' }, 2);

            return new WebsiteSource
            {
                Website    = parts[0],
                Identifier = parts[^1]
            };
        }

        /// <summary>
        /// Formats as "{website}/{identifier]".
        /// </summary>
        public static string Format(string website, string identifier) => $"{website}/{identifier}";

        public override string ToString() => Format(Website, Identifier);
    }
}