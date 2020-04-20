using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a piece of file.
    /// </summary>
    public class Piece
    {
        /// <summary>
        /// Size of piece hash which is 16 bytes.
        /// </summary>
        public const int HashSize = 16;

        /// <summary>
        /// Maximum piece size which is 1 mebibyte.
        /// </summary>
        public const int MaxSize = 1048576;

        /// <summary>
        /// Piece size in bytes, which must be greater than 0 and less than <see cref="MaxSize"/>.
        /// </summary>
        [Required, Range(1, MaxSize)]
        public int Size { get; set; }

        /// <summary>
        /// SHA256 hash of the piece data, truncated to the left bytes of length <see cref="HashSize"/>.
        /// </summary>
        [Required, MinLength(HashSize), MaxLength(HashSize)]
        public byte[] Hash { get; set; }
    }
}