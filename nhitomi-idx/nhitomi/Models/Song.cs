using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a song.
    /// </summary>
    public class Song : SongBase
    {
        [Required, NanokaId]
        public string Id { get; set; }

        [Required]
        public double Score { get; set; }

        [Required]
        public double Length { get; set; }

        [Required]
        public int Bitrate { get; set; }
    }

    public class SongBase
    {
        /// <summary>
        /// First element should be the fully localized primary name.
        /// </summary>
        [Required]
        public string[] Name { get; set; }

        [Required, Range(0, double.MaxValue)]
        public double PreviewTime { get; set; }

        [Required]
        public Dictionary<LanguageType, SongLyrics> Lyrics { get; set; } = new Dictionary<LanguageType, SongLyrics>();
    }
}