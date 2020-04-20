using System.ComponentModel.DataAnnotations;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PeerOffer"/>
    public class PeerOfferMessage : MessageBase
    {
        /// <summary>
        /// ID of the remote client.
        /// i.e. for the offering/answering client, the ID of the answering/offering client.
        /// </summary>
        [MP.Key("clientId")]
        public long ClientId { get; set; }

        /// <summary>
        /// Type of the offering client.
        /// </summary>
        [MP.Key("clientType"), Required, MaxLength(32)]
        public string ClientType { get; set; }

        /// <summary>
        /// Serialized offer information.
        /// </summary>
        [MP.Key("data"), Required, MaxLength(4096)]
        public string Data { get; set; }
    }
}