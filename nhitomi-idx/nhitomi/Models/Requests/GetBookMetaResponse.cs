using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class GetBookMetaResponse
    {
        /// <summary>
        /// User who had initially posted the book.
        /// </summary>
        [Required]
        public User Creator { get; set; }

        /// <summary>
        /// Snapshot of the creation of the book.
        /// </summary>
        [Required]
        public Snapshot CreationSnapshot { get; set; }

        /// <summary>
        /// User who had most recently edited the book.
        /// </summary>
        [Required]
        public User Editor { get; set; }

        /// <summary>
        /// Snapshot of the most recent edit of the book.
        /// </summary>
        [Required]
        public Snapshot EditSnapshot { get; set; }

        /// <summary>
        /// Total number of snapshots of the book.
        /// </summary>
        [Required]
        public int TotalSnapshotCount { get; set; }
    }
}