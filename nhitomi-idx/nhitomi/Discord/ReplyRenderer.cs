using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;

namespace nhitomi.Discord
{
    public interface IReplyRenderer
    {
        /// <summary>
        /// Sends a rendered <see cref="ReplyContent"/> in response to the given command message.
        /// </summary>
        Task<IUserMessage> SendAsync(IUserMessage command, ReplyMessage reply, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies the given message's content with a rendered <see cref="ReplyContent"/>.
        /// </summary>
        Task<bool> ModifyAsync(IUserMessage message, ReplyMessage reply, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Responsible for rendering a static reply message.
    /// </summary>
    public class ReplyRenderer : IReplyRenderer
    {
        readonly ILogger<ReplyRenderer> _logger;

        public ReplyRenderer(ILogger<ReplyRenderer> logger)
        {
            _logger = logger;
        }

        public async Task<IUserMessage> SendAsync(IUserMessage command, ReplyMessage reply, CancellationToken cancellationToken = default)
        {
            var content = await reply.RenderInternalAsync(cancellationToken);

            if (content == null || !content.IsValid)
                return null;

            try
            {
                // try sending in the same channel as the command
                var message = await SendChannelAsync(command.Channel, content);

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"Rendered message {reply.GetType().Name} {message.Id} in channel #{message.Channel}.");

                return message;
            }
            catch (Exception e)
            {
                try
                {
                    // if command channel fails, try sending via dm
                    var channel = await command.Author.GetOrCreateDMChannelAsync();

                    var message = await SendChannelAsync(channel, content);

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"Rendered message {reply.GetType().Name} {message.Id} in channel #{message.Channel}.");

                    return message;
                }
                catch
                {
                    _logger.LogDebug(e, $"Could not render message {reply.GetType().Name}.");

                    // throw original exception
                    ExceptionDispatchInfo.Throw(e);

                    return null;
                }
            }
        }

        static Task<IUserMessage> SendChannelAsync(IMessageChannel channel, ReplyContent content)
            => channel.SendMessageAsync(content.Message, false, content.Embed?.Build());

        public async Task<bool> ModifyAsync(IUserMessage message, ReplyMessage reply, CancellationToken cancellationToken = default)
        {
            var content = await reply.RenderInternalAsync(cancellationToken);

            if (content == null || !content.IsValid)
                return false;

            await message.ModifyAsync(m =>
            {
                m.Content = content.Message;
                m.Embed   = content.Embed?.Build();
            });

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Rendered message {reply.GetType().Name} {message.Id} in channel #{message.Channel}.");

            return true;
        }
    }
}