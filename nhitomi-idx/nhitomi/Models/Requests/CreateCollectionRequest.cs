using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class CreateCollectionRequest : CollectionBase
    {
        /// <summary>
        /// Type of objects in this collection.
        /// </summary>
        [Required]
        public ObjectType Type { get; set; }
    }
}