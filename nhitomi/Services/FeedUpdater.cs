// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Database;

namespace nhitomi.Services
{
    public class FeedUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly IDatabase _database;
        readonly ILogger<FeedUpdater> _logger;

        public FeedUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            MessageFormatter formatter,
            IDatabase database,
            ILogger<FeedUpdater> logger)
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _formatter = formatter;
            _database = database;
            _logger = logger;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins =
            new ConcurrentDictionary<IDoujinClient, IDoujin>();

        async Task<IAsyncEnumerable<IDoujin>> FindNewDoujinsAsync(CancellationToken cancellationToken = default)
        {
            var doujins = Extensions.Interleave(await Task.WhenAll(_clients.Select(async c =>
            {
                IDoujin current = null;

                try
                {
                    if (!_lastDoujins.TryGetValue(c, out var last))
                        _lastDoujins[c] = null;

                    // Get all new doujins up to the last one we know
                    var list =
                        (await c.SearchAsync(null, cancellationToken))
                        .TakeWhile(d => d?.Id != last?.Id);

                    current = await list.FirstOrDefault(cancellationToken) ?? last;

                    if (current != last && last != null)
                        return list;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Exception while searching client '{c.Name}': {e.Message}");

                    current = null;
                }
                finally
                {
                    _lastDoujins[c] = current;

                    _logger.LogDebug($"Most recent doujin: [{c.Name}] {current?.PrettyName ?? "<null>"}");
                }

                return AsyncEnumerable.Empty<IDoujin>();
            })));

            if (_settings.Doujin.MaxFeedUpdatesCount > 0)
                doujins = doujins.Take(_settings.Doujin.MaxFeedUpdatesCount);

            // todo: should be reversed in order
            return doujins;
        }

        async Task SendUpdateAsync(
            IMessageChannel channel,
            IDoujin doujin,
            Embed embed = null,
            bool isFeedDoujin = true)
        {
            try
            {
                var message = await channel.SendMessageAsync(embed: embed ?? _formatter.CreateDoujinEmbed(doujin));

                if (isFeedDoujin)
                    await _formatter.AddFeedDoujinTriggersAsync(message);
                else
                    await _formatter.AddDoujinTriggersAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Exception while sending feed message for doujin '{doujin.OriginalName ?? doujin.PrettyName}'");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // find new doujins
                    var doujins = await FindNewDoujinsAsync(stoppingToken);

                    // find tag feed channels
                    var tagChannels =
                        ((_discord.Socket.GetChannel(_settings.Discord.Guild.FeedCategoryId) as SocketCategoryChannel)
                         ?.Channels.OfType<ITextChannel>() ?? new ITextChannel[0])
                        .ToDictionary(
                            c => c.Name.Replace('-', ' '),
                            c => c);

                    if (tagChannels.Count != 0)
                        _logger.LogDebug($"Found tag feed channels: {string.Join(", ", tagChannels.Keys)}");

                    // find language feed channels
                    var langChannels =
                        ((_discord.Socket.GetChannel(_settings.Discord.Guild.LanguageFeedCategoryId) as
                             SocketCategoryChannel)
                         ?.Channels.OfType<ITextChannel>() ?? new ITextChannel[0])
                        .ToDictionary(
                            c => c.Name.Replace('-', ' '),
                            c => c);

                    if (langChannels.Count != 0)
                        _logger.LogDebug($"Found language feed channels: {string.Join(", ", langChannels.Keys)}");

                    // find tag subscribers
                    var tagSubscriptions =
                        (await _database.GetTagSubscriptionsAsync(stoppingToken))
                        .ToDictionary(
                            s => s.TagName,
                            s => s.UserList);

                    _logger.LogDebug($"Found {tagSubscriptions.Count} tags and " +
                                     $"{tagSubscriptions.Sum(s => s.Value.Count)} subscribers.");

                    await doujins.ForEachAsync(async d =>
                    {
                        try
                        {
                            var embed = _formatter.CreateDoujinEmbed(d);

                            ITextChannel channel;

                            // to prevent notifying the same subscriber multiple times
                            var notifiedSubscribers = new HashSet<ulong>();

                            foreach (var tag in d.Tags)
                            {
                                // tag feeds
                                if (tagChannels.TryGetValue(tag, out channel))
                                    await SendUpdateAsync(channel, d, embed);

                                // tag subscribers
                                if (!tagSubscriptions.TryGetValue(tag, out var userList))
                                    continue;

                                foreach (var user in userList.Where(notifiedSubscribers.Add))
                                {
                                    await SendUpdateAsync(
                                        await _discord.Socket.GetUser(user).GetOrCreateDMChannelAsync(),
                                        d, embed, false);
                                }
                            }

                            // language feed
                            if (langChannels.TryGetValue(d.Language, out channel))
                                await SendUpdateAsync(channel, d, embed);

                            _logger.LogDebug($"Sent feed update '{d.OriginalName ?? d.PrettyName}'");
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e,
                                $"Exception while sending feed update '{d.OriginalName ?? d.PrettyName}'.");
                        }
                    }, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while sending feed updates.");
                }

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.FeedUpdateInterval),
                    stoppingToken);
            }
        }
    }
}