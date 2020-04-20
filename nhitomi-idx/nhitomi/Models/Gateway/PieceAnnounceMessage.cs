using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PieceAnnounce"/>
    public class PieceAnnounceMessage : MessageBase
    {
        /// <summary>
        /// Type of the object that contains the pieces.
        /// </summary>
        [MP.Key("target")]
        public SnapshotTarget Target { get; set; }

        /// <summary>
        /// ID of the object that contains the pieces.
        /// </summary>
        [MP.Key("targetId"), Required, NanokaId]
        public string TargetId { get; set; }

        /// <summary>
        /// Hashes of the pieces.
        /// </summary>
        [MP.Key("hashes"), Required, HashArray]
        public byte[][] Hashes { get; set; }
    }
}