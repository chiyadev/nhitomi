using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class CollectionItemsRequest
    {
        /// <summary>
        /// IDs of items in collection.
        /// </summary>
        [Required]
        public string[] Items { get; set; }
    }
}