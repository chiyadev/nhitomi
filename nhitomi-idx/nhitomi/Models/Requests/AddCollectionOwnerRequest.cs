using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class AddCollectionOwnerRequest
    {
        /// <summary>
        /// ID of the user to add as a co-owner of the collection.
        /// </summary>
        [Required]
        public string UserId { get; set; }
    }
}