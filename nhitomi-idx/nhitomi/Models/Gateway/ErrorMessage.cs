using System.Net;
using MP = MessagePack;

// ReSharper wants OK -> Ok
// ReSharper disable InconsistentNaming

namespace nhitomi.Models.Gateway
{
    /// <summary>
    /// Errors are a subset of HTTP status codes.
    /// </summary>
    public enum ErrorStatusCode
    {
        /// <inheritdoc cref="HttpStatusCode.OK"/>
        OK = HttpStatusCode.OK,

        /// <inheritdoc cref="HttpStatusCode.BadRequest"/>
        BadRequest = HttpStatusCode.BadRequest,

        /// <inheritdoc cref="HttpStatusCode.NotFound"/>
        NotFound = HttpStatusCode.NotFound,

        /// <inheritdoc cref="HttpStatusCode.UnprocessableEntity"/>
        UnprocessableEntity = HttpStatusCode.UnprocessableEntity,

        /// <inheritdoc cref="HttpStatusCode.InternalServerError"/>
        InternalServerError = HttpStatusCode.InternalServerError
    }

    /// <inheritdoc cref="MessageType.Error"/>
    public class ErrorMessage : MessageBase
    {
        public static ErrorMessage OK(short? id)
            => new ErrorMessage { Id = id, Status = ErrorStatusCode.OK };

        public static ErrorMessage BadRequest(short? id, string reason)
            => new ErrorMessage { Id = id, Status = ErrorStatusCode.BadRequest, Reason = reason };

        public static ErrorMessage NotFound(short? id, string reason)
            => new ErrorMessage { Id = id, Status = ErrorStatusCode.NotFound, Reason = reason };

        public static ErrorMessage UnprocessableEntity(short? id, string reason)
            => new ErrorMessage { Id = id, Status = ErrorStatusCode.UnprocessableEntity, Reason = reason };

        public static ErrorMessage InternalServerError(short? id, string reason)
            => new ErrorMessage { Id = id, Status = ErrorStatusCode.InternalServerError, Reason = reason };

        /// <summary>
        /// Status code of the error.
        /// </summary>
        [MP.Key("status")]
        public ErrorStatusCode Status { get; set; } = ErrorStatusCode.OK;

        /// <summary>
        /// Description of the error.
        /// </summary>
        [MP.Key("reason")]
        public string Reason { get; set; }
    }
}