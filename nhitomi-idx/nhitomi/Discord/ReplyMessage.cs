using System.Threading;
using System.Threading.Tasks;

namespace nhitomi.Discord
{
    /// <summary>
    /// Defines the base class for a generic message that represents the reply to a command.
    /// This message is stateless and reference is lost after send.
    /// </summary>
    public abstract class ReplyMessage
    {
        /// <summary>
        /// Synchronous equivalent of <see cref="RenderAsync"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual ReplyContent Render() => null;

        /// <summary>
        /// Renders the contents of this message.
        /// Returning null will invalidate this message, causing it to not send for static replies, or to expire and be deleted for interactive replies.
        /// </summary>
        protected virtual Task<ReplyContent> RenderAsync(CancellationToken cancellationToken = default) => Task.FromResult(Render());

        internal Task<ReplyContent> RenderInternalAsync(CancellationToken cancellationToken = default) => RenderAsync(cancellationToken);
    }
}