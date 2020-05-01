namespace nhitomi.Models
{
    /// <summary>
    /// Represents the image of a page in a book.
    /// </summary>
    public class BookImage
    {
        /// <summary>
        /// Notes on this image.
        /// </summary>
        public ImageNote[] Notes { get; set; }
    }
}