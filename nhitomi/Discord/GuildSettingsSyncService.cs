using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;

namespace nhitomi.Discord
{
    public class GuildSettingsCache : ConcurrentDictionary<ulong, Guild>
    {
        public readonly ConcurrentQueue<ulong> RefreshQueue = new ConcurrentQueue<ulong>();

        public void EnqueueRefresh(IChannel channel)
        {
            if (channel is IGuildChannel guildChannel)
                RefreshQueue.Enqueue(guildChannel.GuildId);
            else
                RefreshQueue.Enqueue(channel.Id);
        }

        public Guild this[IChannel channel]
        {
            get
            {
                if (channel is IGuildChannel guildChannel)
                {
                    if (TryGetValue(guildChannel.GuildId, out var guild))
                        return guild;

                    return new Guild
                    {
                        Id = guildChannel.GuildId
                    };
                }

                return new Guild
                {
                    Id = channel.Id
                };
            }
            set
            {
                if (channel is IGuildChannel guildChannel)
                    this[guildChannel.GuildId] = value;
                else
                    this[channel.Id] = value;
            }
        }
    }

    public class GuildSettingsSyncService : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly GuildSettingsCache _cache;
        readonly DiscordService _discord;

        public GuildSettingsSyncService(IServiceProvider services, GuildSettingsCache cache, DiscordService discord)
        {
            _services = services;
            _cache = cache;
            _discord = discord;

            _discord.GuildAvailable += RefreshGuildAsync;
            _discord.JoinedGuild += RefreshGuildAsync;
        }

        static readonly DependencyFactory<RefreshQueueProcessor> _factory =
            DependencyUtility<RefreshQueueProcessor>.Factory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_cache.RefreshQueue.Count != 0)
                {
                    // process refresh queue
                    using (var scope = _services.CreateScope())
                        await _factory(scope.ServiceProvider).RunAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        Task RefreshGuildAsync(SocketGuild guild)
        {
            _cache.RefreshQueue.Enqueue(guild.Id);

            return Task.CompletedTask;
        }

        sealed class RefreshQueueProcessor
        {
            readonly IDatabase _database;
            readonly GuildSettingsCache _cache;

            public RefreshQueueProcessor(IDatabase database, GuildSettingsCache cache)
            {
                _database = database;
                _cache = cache;
            }

            public async Task RunAsync(CancellationToken cancellationToken = default)
            {
                var ids = new HashSet<ulong>();

                // get all ids in refresh queue
                while (_cache.RefreshQueue.TryDequeue(out var id))
                    ids.Add(id);

                var guilds = await _database.GetGuildsAsync(ids.ToArray(), cancellationToken);

                // update the cache
                foreach (var guild in guilds)
                    _cache[guild.Id] = guild;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _discord.GuildAvailable -= RefreshGuildAsync;
            _discord.JoinedGuild -= RefreshGuildAsync;
        }
    }
}