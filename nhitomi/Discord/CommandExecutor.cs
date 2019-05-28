using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public class CommandExecutor : IMessageHandler
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly DiscordService _discord;
        readonly InteractiveManager _interactive;
        readonly ILogger<CommandExecutor> _logger;

        public CommandExecutor(IServiceProvider services, IOptions<AppSettings> options, DiscordService discord,
            InteractiveManager interactive, ILogger<CommandExecutor> logger)
        {
            _services = services;
            _settings = options.Value;
            _discord = discord;
            _interactive = interactive;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using (var scope = _services.CreateScope())
            {
                // load command modules
                await _discord.Command.AddModulesAsync(typeof(Startup).Assembly, scope.ServiceProvider);
            }

            _logger.LogDebug($"Loaded commands: {string.Join(", ", _discord.Command.Commands.Select(c => c.Name))}");
        }

        public async Task<bool> TryHandleAsync(MessageContext context, CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                case MessageEvent.Create:
                    break;

                default:
                    return false;
            }

            var argIndex = 0;

            // message has command prefix
            if (!context.Message.HasStringPrefix(_settings.Discord.Prefix, ref argIndex) &&
                !context.Message.HasMentionPrefix(context.Client.CurrentUser, ref argIndex))
                return false;

            IResult result;

            // dependency scope
            using (var scope = _services.CreateScope())
            {
                // execute command
                result = await _discord.Command.ExecuteAsync(context, argIndex, scope.ServiceProvider);
            }

            // check for any errors during command execution
            if (result.Error == CommandError.Exception)
            {
                var e = ((ExecuteResult) result).Exception;

                _logger.LogWarning(e, "Exception while handling message {0}.", context.Message.Id);

                // notify the user about this error
                await _interactive.SendInteractiveAsync(new ErrorMessage(e), context, cancellationToken);
            }

            return true;
        }
    }
}