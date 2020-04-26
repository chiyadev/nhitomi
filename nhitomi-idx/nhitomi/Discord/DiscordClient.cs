using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public interface IDiscordClient : IDisposable
    {
        int Latency { get; }
        IReadOnlyCollection<DiscordSocketClient> Shards { get; }
        IReadOnlyCollection<SocketGuild> Guilds { get; }
        IReadOnlyCollection<ISocketPrivateChannel> PrivateChannels { get; }

        void Initialize(CancellationToken cancellationToken = default);

        Task LoginAsync(TokenType type, string token, bool validate = false);
        Task LogoutAsync();

        Task StartAsync();
        Task StopAsync();

        SocketUser GetUser(ulong id);
        DiscordSocketClient GetShardFor(IGuild guild);
    }

    /// <summary>
    /// Represents a Discord client.
    /// </summary>
    public class DiscordClient : DiscordShardedClient, IDiscordClient
    {
        readonly IServiceProvider _services;
        readonly ILogger<DiscordClient> _logger;

        public DiscordClient(IServiceProvider services, ILogger<DiscordClient> logger, IOptionsMonitor<DiscordOptions> options) : base(options.CurrentValue)
        {
            _services = services;
            _logger   = logger;
        }

        public void Initialize(CancellationToken cancellationToken = default)
        {
            var message  = _services.GetService<IDiscordMessageHandler>();
            var reaction = _services.GetService<IDiscordReactionHandler>();

            Log += HandleLogAsync;

            MessageReceived += m =>
            {
                if (m is IUserMessage um)
                    Task.Run(() => message.HandleAsync(um, cancellationToken), cancellationToken);

                return Task.CompletedTask;
            };

            ReactionAdded += (_, __, r) =>
            {
                Task.Run(() => reaction.HandleAsync(r, cancellationToken), cancellationToken);
                return Task.CompletedTask;
            };

            ReactionRemoved += (_, __, r) =>
            {
                Task.Run(() => reaction.HandleAsync(r, cancellationToken), cancellationToken);
                return Task.CompletedTask;
            };
        }

        Task HandleLogAsync(LogMessage message)
        {
            var level = message.Severity switch
            {
                LogSeverity.Debug    => LogLevel.Trace,
                LogSeverity.Verbose  => LogLevel.Debug,
                LogSeverity.Info     => LogLevel.Information,
                LogSeverity.Warning  => LogLevel.Warning,
                LogSeverity.Error    => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,

                _ => LogLevel.None
            };

            _logger.Log(level, message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public new void Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch (NullReferenceException)
            {
                // workaround until we upgrade Discord.Net
                // https://github.com/discord-net/Discord.Net/issues/1492
            }
        }
    }
}