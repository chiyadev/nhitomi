using System.ComponentModel.DataAnnotations;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Discord;
using nhitomi.Storage;

namespace nhitomi.Models
{
    /// <summary>
    /// Contains all internal server configuration values.
    /// </summary>
    public class CompositeConfig
    {
        [Required]
        public ServerOptions Server { get; set; }

        [Required]
        public StorageOptions Storage { get; set; }

        [Required]
        public ElasticOptions Elastic { get; set; }

        [Required]
        public RedisOptions Redis { get; set; }

        [Required]
        public RecaptchaOptions Recaptcha { get; set; }

        [Required]
        public DiscordOptions Discord { get; set; }

        [Required]
        public UserServiceOptions User { get; set; }
    }
}