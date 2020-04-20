using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents an image in an imageboard/booru (synonymous to "post").
    /// </summary>
    public class Image : ImageBase, INanokaObject, IHasUpdatedTime
    {
        public const int ImageMinSize = 100;
        public const int ImageMaxSize = 4096;

        /// <summary>
        /// Image ID.
        /// </summary>
        [Required, NanokaId]
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
        /// Image width in pixels.
        /// </summary>
        [Required, Range(ImageMinSize, ImageMaxSize)]
        public int Width { get; set; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        [Required, Range(ImageMinSize, ImageMaxSize)]
        public int Height { get; set; }

        /// <summary>
        /// Availability score between [0, 1] which indicates how many pieces are available in the network relative to the number of pieces.
        /// </summary>
        [Required]
        public double Availability { get; set; }

        /// <summary>
        /// Total availability score which indicates how many replicas of pieces are available in the network relative to the number of pieces.
        /// </summary>
        [Required]
        public double TotalAvailability { get; set; }

        /// <summary>
        /// Pieces that comprise this image.
        /// </summary>
        [Required, PieceList]
        public Piece[] Pieces { get; set; }
    }

    public class ImageBase
    {
        /// <summary>
        /// Tags on this image.
        /// </summary>
        [Required]
        public Dictionary<ImageTag, string[]> Tags { get; set; } = new Dictionary<ImageTag, string[]>();

        /// <summary>
        /// Sources from where this image was downloaded.
        /// </summary>
        public WebsiteSource[] Sources { get; set; }

        /// <summary>
        /// Material rating.
        /// </summary>
        [Required]
        public MaterialRating Rating { get; set; }
    }
}