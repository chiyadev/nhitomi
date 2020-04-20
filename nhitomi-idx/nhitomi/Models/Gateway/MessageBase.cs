using MessagePack;

namespace nhitomi.Models.Gateway
{
    public enum MessageType
    {
        /// <summary>
        /// Indicates a success or error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Contains gateway information.
        /// </summary>
        Context = 2,

        /// <summary>
        /// Contains peer-to-peer connection information of the client that initiates the connection.
        /// </summary>
        /// <remarks>
        /// Clients that send this message will likely receive <see cref="PeerAnswer"/> as response.
        /// Clients that receive this message should respond with <see cref="PeerAnswer"/>.
        /// </remarks>
        PeerOffer = 3,

        /// <summary>
        /// Contains peer-to-peer connection information of the client that completes the connection initiation.
        /// </summary>
        PeerAnswer = 4,

        /// <summary>
        /// Subscribes to peer offer/answer messages.
        /// </summary>
        PeerSubscribe = 5,

        /// <summary>
        /// Declares that a client is providing pieces.
        /// </summary>
        PieceAnnounce = 6,

        /// <summary>
        /// Declares that a client is no longer providing pieces.
        /// </summary>
        PieceRenounce = 7,

        /// <summary>
        /// Asks which clients are providing a piece.
        /// </summary>
        /// <remarks>
        /// Clients will receive <see cref="PieceLocation"/> as response.
        /// </remarks>
        PieceLocate = 8,

        /// <summary>
        /// Contains information of clients that are providing a piece.
        /// </summary>
        PieceLocation = 9
    }

    /// <summary>
    /// Represents a message used by the gateway.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gateway is a server that is responsible for facilitating peer-to-peer connections and pushing real-time events to the client.
    /// It operates over a WebSocket connection using subprotocol "nhitomi" and communicates using messages that are serialized
    /// using MessagePack and transferred across one or more contiguous frames.
    /// </para>
    /// <para>
    /// An asynchronous hybrid request-response and publish-subscribe model is employed, but it is not strictly enforced.
    /// e.g. <see cref="MessageType.PeerOffer"/> is a form of request but does not guarantee a response.
    /// </para>
    /// <para>
    /// The gateway and all clients must guarantee the following:
    /// - The gateway shall push <see cref="MessageType.Context"/> message to the client immediately after a successful connection.
    /// - The gateway shall respond with <see cref="MessageType.Error"/> for all messages from the client unless otherwise noted in a documentation.
    /// - For every message received that is valid but not processable at the time of receipt, the client should ignore it and the gateway shall respond with an <see cref="MessageType.Error"/>.
    /// - For every message received that is of unknown type or of type that cannot be handled by the receiver, the connection should be aborted.
    /// </para>
    /// </remarks>
    [MessagePackObject]
    [Union((int) MessageType.Error, typeof(ErrorMessage))]
    [Union((int) MessageType.Context, typeof(ContextMessage))]
    [Union((int) MessageType.PeerOffer, typeof(PeerOfferMessage))]
    [Union((int) MessageType.PeerAnswer, typeof(PeerAnswerMessage))]
    [Union((int) MessageType.PeerSubscribe, typeof(PeerSubscribeMessage))]
    [Union((int) MessageType.PieceAnnounce, typeof(PieceAnnounceMessage))]
    [Union((int) MessageType.PieceRenounce, typeof(PieceRenounceMessage))]
    [Union((int) MessageType.PieceLocate, typeof(PieceLocateMessage))]
    [Union((int) MessageType.PieceLocation, typeof(PieceLocationMessage))]
    public abstract class MessageBase
    {
        /// <summary>
        /// Message ID.
        /// </summary>
        /// <remarks>
        /// If this message expects a subsequent response, an ID can be used to pair these related messages.
        /// This is useful for request-response when the connection is being multiplexed for asynchronicity.
        /// </remarks>
        [Key("id")]
        public short? Id { get; set; }
    }
}