using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class NewBookRequest : NewBookContentRequest
    {
        /// <summary>
        /// Book information.
        /// </summary>
        [Required]
        public BookBase Book { get; set; }
    }
}