using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents the image of a page in a book.
    /// </summary>
    public class BookImage
    {
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
}