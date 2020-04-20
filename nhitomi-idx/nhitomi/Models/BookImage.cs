using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    public class BookImage
    {
        /// <summary>
        /// Image width in pixels.
        /// </summary>
        [Required, Range(Image.ImageMinSize, Image.ImageMaxSize)]
        public int Width { get; set; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        [Required, Range(Image.ImageMinSize, Image.ImageMaxSize)]
        public int Height { get; set; }

        /// <summary>
        /// Pieces that comprise this image.
        /// </summary>
        [Required, PieceList]
        public Piece[] Pieces { get; set; }
    }
}