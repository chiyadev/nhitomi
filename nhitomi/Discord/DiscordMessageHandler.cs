using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace nhitomi.Discord
{
    public class DiscordMessageHandler : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly IDiscordClient _client;
        readonly IUserFilter _filter;
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly ILogger<DiscordMessageHandler> _logger;

        readonly CommandService _command;

        public DiscordMessageHandler(IServiceProvider services, IDiscordClient client, IUserFilter filter, IOptionsMonitor<DiscordOptions> options, ILogger<DiscordMessageHandler> logger)
        {
            _services = services;
            _client   = client;
            _filter   = filter;
            _options  = options;
            _logger   = logger;

            // add modules
            _command = new CommandService(options.CurrentValue.Command);
            _command.AddModules(GetType().Assembly);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.MessageReceived += m =>
            {
                Task.Run(() => HandleMessageAsync(m, stoppingToken), stoppingToken);
                return Task.CompletedTask;
            };

            await Task.Delay(-1, stoppingToken);
        }

        int _total;
        int _handled;

        public int Total => _total;
        public int Handled => _handled;

        async Task HandleMessageAsync(IMessage m, CancellationToken cancellationToken = default)
        {
            try
            {
                // must be an user-authored message
                if (!(m is IUserMessage message) || !_filter.HandleUser(m.Author))
                    return;

                var options = _options.CurrentValue;

                // must have command prefix
                if (!CommandUtilities.HasPrefix(m.Content, options.Prefix, options.Command.StringComparison, out var command))
                    return;

                IResult result;

                // execute command
                // (ref-counted service scope is used to keep services alive for interactive messages that spans multiple commands/reactions)
                using (var serviceScope = new RefCountedServiceScope(_services.CreateScope()))
                    result = await _command.ExecuteAsync(command, new nhitomiCommandContext(serviceScope, message, cancellationToken));

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
                _logger.LogWarning(e, $"Exception while handling message {m.Id}.");
            }
            finally
            {
                Interlocked.Increment(ref _total);
            }
        }
    }
}