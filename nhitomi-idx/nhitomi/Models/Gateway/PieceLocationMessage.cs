using System.ComponentModel.DataAnnotations;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PieceLocation"/>
    public class PieceLocationMessage : MessageBase
    {
        /// <summary>
        /// IDs of clients that are providing the located piece.
        /// </summary>
        [MP.Key("clients"), Required]
        public long[] Clients { get; set; }
    }
}