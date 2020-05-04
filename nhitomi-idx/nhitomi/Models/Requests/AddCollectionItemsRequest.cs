using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class AddCollectionItemsRequest : CollectionItemsRequest
    {
        /// <summary>
        /// Position of the inserted items.
        /// </summary>
        [Required]
        public CollectionInsertPosition Position { get; set; }
    }
}