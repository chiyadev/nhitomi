using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using MP = MessagePack;

namespace nhitomi.Models.Gateway
{
    /// <inheritdoc cref="MessageType.Context"/>
    public class ContextMessage : MessageBase
    {
        /// <summary>
        /// ID of the user that the client authenticated as.
        /// </summary>
        [MP.Key("userId"), Required, NanokaId]
        public string UserId { get; set; }

        /// <summary>
        /// ID of the connected client. This is unique for every connection.
        /// </summary>
        [MP.Key("clientId")]
        public long ClientId { get; set; }

        /// <summary>
        /// Maximum message size allowed by clients in bytes.
        /// The gateway is not restricted by this size.
        /// </summary>
        [MP.Key("messageSize")]
        public int MessageSize { get; set; }

        /// <summary>
        /// WebSocket keepalive interval in seconds.
        /// </summary>
        [MP.Key("keepAliveInterval")]
        public int KeepAliveInterval { get; set; }
    }
}