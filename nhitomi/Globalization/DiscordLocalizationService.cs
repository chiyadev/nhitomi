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
using nhitomi.Discord;

namespace nhitomi.Globalization
{
    public interface ILocalization
    {
        void EnqueueRefresh(IDiscordContext context);

        Localization this[IDiscordContext context] { get; }
    }

    public class LocalizationCache : ConcurrentDictionary<ulong, Localization>, ILocalization
    {
        public readonly ConcurrentQueue<ulong> RefreshQueue = new ConcurrentQueue<ulong>();

        public void EnqueueRefresh(IDiscordContext context)
        {
            if (context.Channel is IGuildChannel channel && channel.Guild != null)
                RefreshQueue.Enqueue(channel.Guild.Id);
        }

        public Localization this[IDiscordContext context] =>
            context.Channel is IGuildChannel channel && TryGetValue(channel.Guild?.Id ?? 0, out var localization)
                ? localization
                : Localization.Default;
    }

    public class DiscordLocalizationService : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly LocalizationCache _cache;
        readonly DiscordService _discord;

        public DiscordLocalizationService(IServiceProvider services, ILocalization localization, DiscordService discord)
        {
            _services = services;
            _cache = localization as LocalizationCache ??
                     throw new ArgumentException($"{nameof(localization)} must be {nameof(LocalizationCache)}");
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

            _discord.GuildAvailable -= RefreshGuildAsync;
            _discord.JoinedGuild -= RefreshGuildAsync;
        }
    }
}