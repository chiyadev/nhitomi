using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a basic git commit.
    /// </summary>
    public class GitCommit
    {
        /// <summary>
        /// Commit hash.
        /// </summary>
        [Required]
        public string Hash { get; set; }

        /// <summary>
        /// Shortened commit hash, which is the first 7 characters of <see cref="Hash"/>.
        /// </summary>
        [Required]
        public string ShortHash { get; set; }

        /// <summary>
        /// Name of the author.
        /// </summary>
        [Required]
        public string Author { get; set; }

        /// <summary>
        /// Time of the commit.
        /// </summary>
        [Required]
        public DateTime Time { get; set; }

        /// <summary>
        /// Message of the commit.
        /// </summary>
        [Required]
        public string Message { get; set; }
    }
}