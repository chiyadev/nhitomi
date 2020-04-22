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
    }

    public class DiscordMessageHandler : IDiscordMessageHandler
    {
        readonly IServiceProvider _services;
        readonly IDiscordLocaleProvider _locale;
        readonly IUserFilter _filter;
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly ILogger<DiscordMessageHandler> _logger;

        readonly CommandService _command;

        public DiscordMessageHandler(IServiceProvider services, IDiscordLocaleProvider locale, IUserFilter filter, IOptionsMonitor<DiscordOptions> options, ILogger<DiscordMessageHandler> logger)
        {
            _services = services;
            _locale   = locale;
            _filter   = filter;
            _options  = options;
            _logger   = logger;

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
                using (var serviceScope = new RefCountedServiceScope(_services.CreateScope()))
                {
                    var context = new nhitomiCommandContext(serviceScope, message, cancellationToken);

                    // set locale
                    await _locale.SetAsync(context, cancellationToken);

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
    }
}