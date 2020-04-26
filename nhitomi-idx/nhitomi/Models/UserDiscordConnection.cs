using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Discord OAuth2 connection information of a user.
    /// </summary>
    public class UserDiscordConnection
    {
        [Required]
        public ulong Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public int Discriminator { get; set; }

        [Required]
        public bool Verified { get; set; }

        [Required]
        public string Email { get; set; }
    }
}