using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace nhitomi.Discord
{
    public class MessageListenArgs
    {
        public IUser User { get; set; }
        public IMessageChannel Channel { get; set; }
        public string Message { get; set; }
        public TimeSpan? Timeout { get; set; }

        internal (ulong, ulong) Key => (User.Id, Channel.Id);
    }

    public interface IDiscordMessageHandler : IDisposable
    {
        int Total { get; }
        int Handled { get; }

        Task HandleAsync(IUserMessage message, CancellationToken cancellationToken = default);
        Task OnDeletedAsync(ulong messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins listening for a message from a user in a specific channel.
        /// Listeners bypass all user filters and prevent the message from propagating to command handlers if matched.
        /// </summary>
        Task<IUserMessage> ListenAsync(MessageListenArgs args, CancellationToken cancellationToken = default);
    }

    public class DiscordMessageHandler : IDiscordMessageHandler
    {
        readonly IServiceProvider _services;
        readonly IDiscordUserHandler _user;
        readonly IUserFilter _filter;
        readonly IInteractiveManager _interactive;
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly ILogger<DiscordMessageHandler> _logger;

        readonly CommandService _command;

        public DiscordMessageHandler(IServiceProvider services, IDiscordUserHandler user, IUserFilter filter, IInteractiveManager interactive, IOptionsMonitor<DiscordOptions> options, ILogger<DiscordMessageHandler> logger)
        {
            _services    = services;
            _user        = user;
            _filter      = filter;
            _interactive = interactive;
            _options     = options;
            _logger      = logger;

            // add modules
            _command = new CommandService(options.CurrentValue.Command);
            _command.AddModules(GetType().Assembly);
        }

        int _total;
        int _handled;

        public int Total => _total;
        public int Handled => _handled;

        public async Task HandleAsync(IUserMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_listeners.TryRemove((message.Author.Id, message.Id), out var completion))
                {
                    completion.TrySetResult(message);

                    Interlocked.Increment(ref _handled);
                    return;
                }

                if (!_filter.HandleUser(message.Author))
                    return;

                var options = _options.CurrentValue;

                // must have command prefix
                if (!CommandUtilities.HasPrefix(message.Content, options.Prefix, options.Command.StringComparison, out var command))
                    return;

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"Command received '{command}' in #{message.Channel} by {message.Author}.");

                IResult result;

                // ref-counted service scope is used to keep services alive for interactive messages that span multiple commands/reactions
                using (var scope = new RefCountedServiceScope(_services.CreateScope()))
                {
                    var context = new nhitomiCommandContext(scope, message, cancellationToken);

                    await _user.SetAsync(context, cancellationToken);

                    result = await _command.ExecuteAsync(command, context);
                }

                switch (result)
                {
                    case ExecutionFailedResult executionFailed:
                        _logger.LogWarning(executionFailed.Exception, executionFailed.Reason);
                        break;

                    case SuccessfulResult successful:
                        if (successful.IsSuccessful)
                            Interlocked.Increment(ref _handled);

                        break;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while handling message {message.Id}.");
            }
            finally
            {
                Interlocked.Increment(ref _total);
            }
        }

        public async Task OnDeletedAsync(ulong messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                // auto dispose interactives when deleted
                var message = _interactive.GetMessage(messageId);

                if (message != null)
                    await message.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, $"Exception while disposing interactive message {messageId}.");
            }
        }

        // (userId, channelId)
        readonly ConcurrentDictionary<(ulong, ulong), TaskCompletionSource<IUserMessage>> _listeners = new ConcurrentDictionary<(ulong, ulong), TaskCompletionSource<IUserMessage>>();

        public async Task<IUserMessage> ListenAsync(MessageListenArgs args, CancellationToken cancellationToken = default)
        {
            args.Timeout ??= _options.CurrentValue.Interactive.MessageListenTimeout;

            using var rootCts   = args.Timeout == null ? new CancellationTokenSource() : new CancellationTokenSource(args.Timeout.Value);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(rootCts.Token, cancellationToken);

            cancellationToken = linkedCts.Token;

            if (!_listeners.TryGetValue(args.Key, out var completion))
                _listeners[args.Key] = completion = new TaskCompletionSource<IUserMessage>();

            var sent     = null as IUserMessage;
            var received = null as IUserMessage;

            try
            {
                await using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
                {
                    if (!string.IsNullOrEmpty(args.Message))
                        sent = await args.Channel.SendMessageAsync(args.Message);

                    return received = await completion.Task;
                }
            }
            finally
            {
                _listeners.TryRemove(args.Key, out _);
                completion?.TrySetCanceled();

                try
                {
                    if (sent != null) await sent.DeleteAsync();
                    if (received != null) await received.DeleteAsync();
                }
                catch
                {
                    // ignored
                }
            }
        }

        public void Dispose()
        {
            foreach (var (key, listener) in _listeners)
            {
                _listeners.TryRemove(key, out _);
                listener.TrySetCanceled();
            }
        }
    }
}