using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a note on an image.
    /// Notes can be used to annotate images with text, usually for translation.
    /// </summary>
    public class ImageNote : ImageNoteBase, IHasId
    {
        /// <summary>
        /// Note ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }
    }

    public class ImageNoteBase
    {
        /// <summary>
        /// X position in pixels from the top-left.
        /// </summary>
        [Required]
        public int X { get; set; }

        /// <summary>
        /// Y position in pixels from the top-left.
        /// </summary>
        [Required]
        public int Y { get; set; }

        /// <summary>
        /// Width in pixels.
        /// </summary>
        [Required]
        public int Width { get; set; }

        /// <summary>
        /// Height in pixels.
        /// </summary>
        [Required]
        public int Height { get; set; }

        /// <summary>
        /// Content text that supports markdown.
        /// </summary>
        [Required, MinLength(5), MaxLength(256)]
        public string Content { get; set; }
    }
}