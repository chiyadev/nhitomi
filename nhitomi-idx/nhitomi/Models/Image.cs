using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

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
        /// Image file size in bytes.
        /// This may be null if this image is unavailable.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// SHA256 hash of the piece data, truncated to the first 16 bytes.
        /// This may be null if this image is unavailable.
        /// </summary>
        [MinLength(16), MaxLength(16)]
        public byte[] Hash { get; set; }

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
        [Required]
        public Dictionary<ImageTag, string[]> Tags { get; set; } = new Dictionary<ImageTag, string[]>();

        /// <summary>
        /// Material rating.
        /// </summary>
        [Required]
        public MaterialRating Rating { get; set; }

        /// <summary>
        /// Sources from where this image was downloaded.
        /// </summary>
        public WebsiteSource[] Sources { get; set; }
    }

    public enum ImageTag
    {
        /// <summary>
        /// Tag has no specific type.
        /// </summary>
        Tag = 0,

        /// <summary>
        /// Tag is an artist.
        /// </summary>
        Artist = 1,

        /// <summary>
        /// Tag is a character.
        /// </summary>
        Character = 2,

        /// <summary>
        /// Tag is a copyright.
        /// </summary>
        Copyright = 3,

        /// <summary>
        /// Tag is metadata.
        /// </summary>
        Metadata = 4,

        /// <summary>
        /// Tag references a pool name.
        /// </summary>
        Pool = 5
    }
}