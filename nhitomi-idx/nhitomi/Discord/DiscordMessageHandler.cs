using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace nhitomi.Discord
{
    public interface IDiscordMessageHandler
    {
        int Total { get; }
        int Handled { get; }

        Task HandleAsync(IUserMessage message, CancellationToken cancellationToken = default);
        Task OnDeletedAsync(ulong messageId, CancellationToken cancellationToken = default);
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
                if (!_filter.HandleUser(message.Author))
                    return;

                var options = _options.CurrentValue;

                // must have command prefix
                if (!CommandUtilities.HasPrefix(message.Content, options.Prefix, options.Command.StringComparison, out var command))
                    return;

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"Command received '{command}' in #{message.Channel} by {message.Author}.");

                IResult result;

                // ref-counted service scope is used to keep services alive for interactive messages that spans multiple commands/reactions
                using (var context = new nhitomiCommandContext(new RefCountedServiceScope(_services.CreateScope()), message, cancellationToken))
                {
                    // set user
                    await _user.SetAsync(context, cancellationToken);

                    // execute command
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
    }
}