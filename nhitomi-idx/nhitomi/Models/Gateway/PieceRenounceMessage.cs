using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PieceRenounce"/>
    public class PieceRenounceMessage : MessageBase
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
    }
}