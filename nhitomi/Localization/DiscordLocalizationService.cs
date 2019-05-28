using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Core;
using nhitomi.Discord;

namespace nhitomi.Localization
{
    public interface ILocalization
    {
        Localization this[ICommandContext context] { get; }
    }

    public class LocalizationCache : ConcurrentDictionary<ulong, Localization>, ILocalization
    {
        public readonly ConcurrentQueue<ulong> RefreshQueue = new ConcurrentQueue<ulong>();

        public Localization this[ICommandContext context] =>
            TryGetValue(context.Guild?.Id ?? 0, out var localization)
                ? localization
                : Localization.Default;
    }

    public class DiscordLocalizationService : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly LocalizationCache _cache;
        readonly DiscordService _discord;

        public DiscordLocalizationService(IServiceProvider services, LocalizationCache cache, DiscordService discord)
        {
            _cache = cache;
            _discord = discord;
            _services = services;

            _discord.Socket.GuildAvailable += RefreshGuildAsync;
            _discord.Socket.JoinedGuild += RefreshGuildAsync;
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
            readonly LocalizationCache _cache;

            public RefreshQueueProcessor(IDatabase database, LocalizationCache cache)
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
                    _cache[guild.Id] = Localization.GetLocalization(guild.Language);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            _discord.Socket.GuildAvailable -= RefreshGuildAsync;
            _discord.Socket.JoinedGuild -= RefreshGuildAsync;
        }
    }
}