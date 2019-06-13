using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public class FeedChannelUpdateService : BackgroundService
    {
        readonly IServiceProvider _services;
        readonly DiscordService _discord;

        public FeedChannelUpdateService(IServiceProvider services, DiscordService discord)
        {
            _services = services;
            _discord = discord;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.WaitForReadyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                using (var updater = DependencyUtility<FeedChannelUpdater>.Factory(scope.ServiceProvider))
                    await updater.RunAsync(stoppingToken);

                // sleep
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        sealed class FeedChannelUpdater : IDisposable
        {
            readonly IDatabase _db;
            readonly DiscordService _discord;
            readonly InteractiveManager _interactive;
            readonly ILogger<FeedChannelUpdater> _logger;
            readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

            public FeedChannelUpdater(IDatabase db, DiscordService discord, InteractiveManager interactive,
                ILogger<FeedChannelUpdater> logger)
            {
                _db = db;
                _discord = discord;
                _interactive = interactive;
                _logger = logger;
            }

            public async Task RunAsync(CancellationToken cancellationToken = default)
            {
                var feedChannels = await _db.GetFeedChannelsAsync(cancellationToken);

                // send updates to channel concurrently
                await Task.WhenAll(feedChannels.Select(c => RunChannelAsync(c, cancellationToken)));

                // save last updated doujin
                await _db.SaveAsync(cancellationToken);
            }

            const int _chunkLoadSize = 10;

            async Task RunChannelAsync(FeedChannel channel, CancellationToken cancellationToken = default)
            {
                try
                {
                    var context = new FeedUpdateContext
                    {
                        Client = _discord,
                        Channel = _discord.GetGuild(channel.GuildId)?.GetTextChannel(channel.Id),
                        GuildSettings = channel.Guild
                    };

                    // no tags or channel not available
                    if (channel.Tags == null || channel.Tags.Count == 0 ||
                        context.Channel == null)
                        return;

                    var tagIds = channel.Tags.Select(t => t.TagId).ToArray();

                    var queue = new Queue<Doujin>();

                    while (true)
                    {
                        if (queue.Count == 0)
                        {
                            // cannot access dbcontext from multiple threads
                            await _semaphore.WaitAsync(cancellationToken);
                            try
                            {
                                var doujins = await _db.GetDoujinsAsync(q =>
                                {
                                    q = q.Where(d => d.ProcessTime > channel.LastDoujin.ProcessTime);

                                    switch (channel.WhitelistType)
                                    {
                                        case FeedChannelWhitelistType.Any:
                                            q = q.Where(d => d.Tags.Any(x => tagIds.Contains(x.TagId)));
                                            break;

                                        case FeedChannelWhitelistType.All:
                                            q = q.Where(d => d.Tags.All(x => tagIds.Contains(x.TagId)));
                                            break;
                                    }

                                    return q
                                        .OrderBy(d => d.ProcessTime)
                                        .Take(_chunkLoadSize);
                                }, cancellationToken);

                                foreach (var d in doujins)
                                    queue.Enqueue(d);
                            }
                            finally
                            {
                                _semaphore.Release();
                            }
                        }

                        // no more doujin
                        if (!queue.TryDequeue(out var doujin) || doujin == null)
                            break;

                        // send doujin interactive
                        using (context.BeginTyping())
                        {
                            // make updates more even
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                            await _interactive.SendInteractiveAsync(
                                new DoujinMessage(doujin),
                                context,
                                cancellationToken);
                        }

                        // set last sent doujin
                        channel.LastDoujin = doujin;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed while updating feed channel {0}.", channel.Id);
                }
            }

            sealed class FeedUpdateContext : IDiscordContext
            {
                public IDiscordClient Client { get; set; }
                public IUserMessage Message => null;
                public IMessageChannel Channel { get; set; }
                public IUser User => null;
                public Guild GuildSettings { get; set; }
            }

            public void Dispose() => _semaphore.Dispose();
        }
    }
}