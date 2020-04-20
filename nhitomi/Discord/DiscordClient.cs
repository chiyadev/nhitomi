using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public interface IDiscordClient
    {
        event Func<SocketMessage, Task> MessageReceived;
        event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionAdded;
        event Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> ReactionRemoved;

        int Latency { get; }
        IReadOnlyCollection<DiscordSocketClient> Shards { get; }
        IReadOnlyCollection<SocketGuild> Guilds { get; }
        IReadOnlyCollection<ISocketPrivateChannel> PrivateChannels { get; }

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
        readonly ILogger<DiscordClient> _logger;

        public DiscordClient(ILogger<DiscordClient> logger, IOptions<DiscordOptions> options) : base(options.Value)
        {
            _logger = logger;

            Log += HandleLogAsync;
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
    }
}