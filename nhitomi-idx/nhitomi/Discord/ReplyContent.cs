using Discord;

namespace nhitomi.Discord
{
    /// <summary>
    /// Rendered view of a reply message.
    /// </summary>
    public sealed class ReplyContent
    {
        /// <summary>
        /// Message string.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Embed content.
        /// </summary>
        public EmbedBuilder Embed { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(Message) || Embed != null;
    }
}