using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class GetInfoAuthenticatedResponse : GetInfoResponse
    {
        /// <summary>
        /// Currently authenticated user information, or null if not authenticated.
        /// </summary>
        [Required]
        public User User { get; set; }
    }
}