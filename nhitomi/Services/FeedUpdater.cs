// Copyright (c) 2018-2019 fate/loli
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

namespace nhitomi.Services
{
    public class FeedUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly ILogger<FeedUpdater> _logger;

        public FeedUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            MessageFormatter formatter,
            ILogger<FeedUpdater> logger)
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _formatter = formatter;
            _logger = logger;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins =
            new ConcurrentDictionary<IDoujinClient, IDoujin>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Concurrently find recent uploads
                var newDoujins = Extensions.Interleave(await Task.WhenAll(_clients.Select(async c =>
                {
                    IDoujin current = null;

                    try
                    {
                        if (!_lastDoujins.TryGetValue(c, out var last))
                            _lastDoujins[c] = null;

                        // Get all new doujins up to the last one we know
                        var list =
                            (await c.SearchAsync(null, stoppingToken))
                            .TakeWhile(d => d?.Id != last?.Id);

                        current = await list.FirstOrDefault(stoppingToken) ?? last;

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
                    newDoujins = newDoujins.Take(_settings.Doujin.MaxFeedUpdatesCount);

                // Get feed channels
                var channels =
                    (_discord.Socket.GetChannel(_settings.Discord.Guild.FeedCategoryId) as SocketCategoryChannel)
                    ?.Channels.OfType<ITextChannel>().ToArray();

                if (channels != null && channels.Length != 0)
                {
                    _logger.LogDebug(
                        $"Found {channels.Length} feed channels: {string.Join(", ", channels.Select(c => c.Name))}");

                    // Send new updates
                    await newDoujins.ForEachAsync(async d =>
                    {
                        if (d.Tags == null)
                            return;

                        var tags = TagsToChannels(d.Tags).ToArray();

                        foreach (var channel in channels.Where(c => tags.Contains(c.Name)))
                        {
                            try
                            {
                                var message = await channel.SendMessageAsync(embed: _formatter.CreateDoujinEmbed(d));
                                await _formatter.AddFeedDoujinTriggersAsync(message);
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning(e,
                                    $"Exception while sending feed message for doujin '{d.OriginalName ?? d.PrettyName}'");
                            }
                        }

                        _logger.LogDebug($"Sent doujin update '{d.OriginalName ?? d.PrettyName}'");
                    }, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("No feed channels were found.");
                }

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.FeedUpdateInterval),
                    stoppingToken);
            }
        }

        static IEnumerable<string> TagsToChannels(IEnumerable<string> tags) =>
            tags.Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t
                    .ToLowerInvariant()
                    .Replace(' ', '-'));
    }
}