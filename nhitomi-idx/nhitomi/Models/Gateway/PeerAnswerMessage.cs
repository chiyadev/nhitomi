using System.ComponentModel.DataAnnotations;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PeerAnswer"/>
    public class PeerAnswerMessage : MessageBase
    {
        /// <summary>
        /// ID of the remote client.
        /// i.e. for the answering/offering client, the ID of the offering/answering client.
        /// </summary>
        [MP.Key("clientId")]
        public long ClientId { get; set; }

        /// <summary>
        /// Type of the answering client, which should be the same or compatible with the offering client.
        /// </summary>
        [MP.Key("clientType"), Required, MaxLength(32)]
        public string ClientType { get; set; }

        /// <summary>
        /// Whether the offer is accepted or not.
        /// </summary>
        [MP.Key("accepted")]
        public bool Accepted { get; set; }

        /// <summary>
        /// If the offer was not accepted, the reason describing why.
        /// </summary>
        [MP.Key("reason"), MaxLength(4096)]
        public string Reason { get; set; }

        /// <summary>
        /// Serialized answer information.
        /// </summary>
        [MP.Key("data"), Required, MaxLength(4096)]
        public string Data { get; set; }
    }
}