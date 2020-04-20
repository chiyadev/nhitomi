using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.PeerSubscribe"/>
    public class PeerSubscribeMessage : MessageBase
    {
        /// <summary>
        /// Whether the subscription is enabled or not.
        /// </summary>
        [MP.Key("enabled")]
        public bool Enabled { get; set; }
    }
}