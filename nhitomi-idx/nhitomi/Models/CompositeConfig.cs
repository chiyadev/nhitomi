using System.ComponentModel.DataAnnotations;
using nhitomi.Database;

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
        public SnapshotServiceOptions Snapshot { get; set; }

        [Required]
        public UserServiceOptions User { get; set; }

        [Required]
        public BookServiceOptions Book { get; set; }

        [Required]
        public VoteServiceOptions Vote { get; set; }

        [Required]
        public GatewayOptions Gateway { get; set; }

        [Required]
        public PieceStoreOptions Piece { get; set; }
    }
}