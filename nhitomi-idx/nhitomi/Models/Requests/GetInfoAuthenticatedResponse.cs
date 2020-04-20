using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class GetInfoAuthenticatedResponse : GetInfoResponse
    {
        /// <summary>
        /// Information of the currently authenticated user, or null if unauthenticated.
        /// </summary>
        [Required]
        public User User { get; set; }
    }
}