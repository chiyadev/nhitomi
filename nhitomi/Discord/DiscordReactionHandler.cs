using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nhitomi.Discord
{
    public class DiscordReactionHandler : BackgroundService
    {
        readonly IDiscordClient _client;
        readonly IUserFilter _filter;
        readonly IInteractiveManager _interactive;
        readonly ILogger<DiscordReactionHandler> _logger;

        public DiscordReactionHandler(IDiscordClient client, IUserFilter filter, IInteractiveManager interactive, ILogger<DiscordReactionHandler> logger)
        {
            _client      = client;
            _filter      = filter;
            _interactive = interactive;
            _logger      = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // reaction add/remove is the same
            _client.ReactionAdded += (_, __, r) =>
            {
                Task.Run(() => HandleReactionAsync(r, stoppingToken), stoppingToken);
                return Task.CompletedTask;
            };

            _client.ReactionRemoved += (_, __, r) =>
            {
                Task.Run(() => HandleReactionAsync(r, stoppingToken), stoppingToken);
                return Task.CompletedTask;
            };

            await Task.Delay(-1, stoppingToken);
        }

        int _total;
        int _handled;

        public int Total => _total;
        public int Handled => _handled;

        async Task HandleReactionAsync(SocketReaction reaction, CancellationToken cancellationToken = default)
        {
            try
            {
                // get reactor user
                var user = reaction.User.IsSpecified ? reaction.User.Value : _client.GetUser(reaction.UserId);

                if (!_filter.HandleUser(user))
                    return;

                // get trigger of reacted interactive
                var trigger = _interactive.GetTrigger(reaction.MessageId, reaction.Emote);

                if (trigger == null)
                    return;

                if (await trigger.InvokeAsync(cancellationToken))
                    Interlocked.Increment(ref _handled);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception while handling reaction {reaction.Emote} on message {reaction.MessageId}.");
            }
            finally
            {
                Interlocked.Increment(ref _total);
            }
        }
    }
}