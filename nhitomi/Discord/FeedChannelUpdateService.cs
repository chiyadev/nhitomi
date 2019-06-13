using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public class FeedChannelUpdateService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly IServiceProvider _services;
        readonly DiscordService _discord;
        readonly ILogger<FeedChannelUpdateService> _logger;

        public FeedChannelUpdateService(IOptions<AppSettings> options, IServiceProvider services,
            DiscordService discord,
            ILogger<FeedChannelUpdateService> logger)
        {
            _settings = options.Value;
            _services = services;
            _discord = discord;
            _logger = logger;
        }

        public readonly ConcurrentDictionary<ulong, Task> UpdaterTasks = new ConcurrentDictionary<ulong, Task>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.Feed.Enabled)
                return;

            await _discord.WaitForReadyAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    // get all feed channels
                    var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
                    var feedChannels = await db.GetFeedChannelsAsync(stoppingToken);

                    // start updater tasks for channels that aren't being updated
                    var tasks = feedChannels
                        .Where(c => !UpdaterTasks.ContainsKey(c.Id))
                        .Select(c => UpdaterTasks[c.Id] = RunChannelUpdateAsync(c.GuildId, c.Id, stoppingToken));

                    foreach (var task in tasks)
                        _ = Task.Run(() => task, stoppingToken);
                }

                // sleep
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        readonly DependencyFactory<FeedChannelUpdater> _updaterFactory = DependencyUtility<FeedChannelUpdater>.Factory;

        async Task RunChannelUpdateAsync(ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                // sleep
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);

                using (var scope = _services.CreateScope())
                {
                    try
                    {
                        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

                        // get feed channel entity
                        var channel = await db.GetFeedChannelAsync(guildId, channelId, cancellationToken);

                        if (channel == null)
                            break;

                        // update channel
                        var updater = _updaterFactory(scope.ServiceProvider);

                        if (channel.Tags != null && channel.Tags.Count != 0 &&
                            await updater.UpdateAsync(channel, cancellationToken))
                            continue;

                        // failed to update channel because feed channel wasn't configured correctly
                        _logger.LogInformation("Feed channel {0} of guild {1} is unavailable.",
                            channel.GuildId, channel.Id);

                        db.Remove(channel);

                        await db.SaveAsync(cancellationToken);

                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to update feed channel {0}.", channelId);
                    }
                }
            }

            // stop updating this channel
            UpdaterTasks.TryRemove(channelId, out _);
        }

        sealed class FeedChannelUpdater
        {
            readonly IDatabase _db;
            readonly DiscordService _discord;
            readonly InteractiveManager _interactive;
            readonly ILogger<FeedChannelUpdater> _logger;

            public FeedChannelUpdater(IDatabase db, DiscordService discord, InteractiveManager interactive,
                ILogger<FeedChannelUpdater> logger)
            {
                _db = db;
                _discord = discord;
                _interactive = interactive;
                _logger = logger;
            }

            const int _loadChunkSize = 10;
            const int _maxSendCount = 50;

            public async Task<bool> UpdateAsync(FeedChannel channel, CancellationToken cancellationToken = default)
            {
                // get discord channel
                var context = new FeedUpdateContext
                {
                    Client = _discord,
                    Channel = _discord.GetGuild(channel.GuildId)?.GetTextChannel(channel.Id),
                    GuildSettings = channel.Guild
                };

                var tagIds = channel.Tags.Select(t => t.TagId).ToArray();

                var queue = new Queue<Doujin>();

                for (var i = 0; i < _maxSendCount; i++)
                {
                    if (queue.Count == 0)
                    {
                        var doujins = await _db.GetDoujinsAsync(
                            q => query(q).Take(_loadChunkSize), // load in chunks
                            cancellationToken);

                        foreach (var d in doujins)
                            queue.Enqueue(d);
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
                            new DoujinMessage(doujin, true),
                            context,
                            cancellationToken);

                        _logger.LogInformation("Sent feed update of doujin {0} '{1}'.",
                            doujin.Id, doujin.OriginalName);
                    }

                    channel.LastDoujin = doujin;
                }

                // set last sent doujin to the latest value
                channel.LastDoujin =
                    (await _db.GetDoujinsAsync(q => query(q).Take(1), cancellationToken))[0]
                    ?? channel.LastDoujin;

                await _db.SaveAsync(cancellationToken);

                _logger.LogInformation("Feed channel {0} is now at doujin {1}.",
                    channel.Id, channel.LastDoujin.Id);

                return true;

                IQueryable<Doujin> query(IQueryable<Doujin> q)
                {
                    q = q
                        .AsNoTracking()
                        .Where(d => d.ProcessTime > channel.LastDoujin.ProcessTime);

                    switch (channel.WhitelistType)
                    {
                        case FeedChannelWhitelistType.Any:
                            q = q.Where(d => d.Tags.Any(x => tagIds.Contains(x.TagId)));
                            break;

                        case FeedChannelWhitelistType.All:
                            q = q.Where(d => d.Tags.All(x => tagIds.Contains(x.TagId)));
                            break;
                    }

                    return q.OrderBy(d => d.ProcessTime);
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
        }
    }
}