using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models.Requests
{
    public class NewBookRequest
    {
        /// <summary>
        /// Book information.
        /// </summary>
        [Required]
        public BookBase Book { get; set; }

        /// <summary>
        /// Pages in the content.
        /// </summary>
        [Required, MaxLength(512)]
        public BookImage[] Pages { get; set; }

        /// <summary>
        /// Thumbnail image data.
        /// </summary>
        /// <remarks>
        /// Image must be less than 64 KiB and at least 500 pixels in width and height.
        /// </remarks>
        [Required, Image(Format = ImageFormat.Jpeg, MaxLength = 65536, MinWidth = 500, MinHeight = 500)]
        public byte[] ThumbnailImage { get; set; }
    }
}