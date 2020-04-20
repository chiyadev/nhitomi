using System.ComponentModel.DataAnnotations;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PieceLocate"/>
    public class PieceLocateMessage : MessageBase
    {
        /// <summary>
        /// Hash of the piece to locate.
        /// </summary>
        [MP.Key("hash"), Required, MinLength(Piece.HashSize), MaxLength(Piece.HashSize)]
        public byte[] Hash { get; set; }
    }
}