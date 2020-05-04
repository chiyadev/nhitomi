using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using nhitomi.Scrapers;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents an image "post" in an imageboard/booru.
    /// </summary>
    public class Image : ImageBase, IHasId, IHasUpdatedTime
    {
        /// <summary>
        /// Image ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this image was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this image was updated.
        /// </summary>
        [Required]
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// Notes on this image.
        /// </summary>
        public ImageNote[] Notes { get; set; }
    }

    public class ImageBase
    {
        /// <summary>
        /// Tags on this image.
        /// </summary>
        [Required, SanitizedTags]
        public Dictionary<ImageTag, string[]> Tags { get; set; } = new Dictionary<ImageTag, string[]>();

        /// <summary>
        /// Material rating.
        /// </summary>
        [Required]
        public MaterialRating Rating { get; set; }

        /// <summary>
        /// Source from where this image was downloaded.
        /// </summary>
        [Required]
        public ScraperType Source { get; set; }
    }

    public enum ImageTag
    {
        /// <summary>
        /// Tag is an artist.
        /// </summary>
        Artist = 0,

        /// <summary>
        /// Tag is a character.
        /// </summary>
        Character = 1,

        /// <summary>
        /// Tag is a copyright.
        /// </summary>
        Copyright = 2,

        /// <summary>
        /// Tag is metadata.
        /// </summary>
        Metadata = 3,

        /// <summary>
        /// Tag references a pool name.
        /// </summary>
        Pool = 4,

        /// <summary>
        /// Tag is generic.
        /// </summary>
        Tag = 5
    }
}