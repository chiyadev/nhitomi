// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class FeedUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly DiscordService _discord;
        readonly InteractiveScheduler _interactive;
        readonly JsonSerializer _json;
        readonly ILogger<FeedUpdater> _logger;

        public FeedUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            InteractiveScheduler interactive,
            JsonSerializer json,
            ILogger<FeedUpdater> logger)
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _interactive = interactive;
            _json = json;
            _logger = logger;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins =
            new ConcurrentDictionary<IDoujinClient, IDoujin>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Finding doujins.");

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
                        _logger.LogWarning(e, $"Exception while listing client '{c.Name}': {e.Message}");

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

                _logger.LogDebug("Finding feed channels.");

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
                        try
                        {
                            var tags = TagsToChannels(d.Tags).ToArray();

                            foreach (var channel in channels.Where(c => tags.Contains(c.Name)))
                            {
                                await _interactive.CreateInteractiveAsync(
                                    null,
                                    await channel.SendMessageAsync(
                                        $"**{d.Source.Name}**: __{d.Id}__",
                                        embed: MessageFormatter.EmbedDoujin(d)),
                                    add => add(
                                        // Heart reaction
                                        ("\u2764", async r =>
                                            {
                                                var requester = _discord.Socket.GetUser(r.UserId);

                                                await DoujinModule.ShowDoujin(
                                                    _interactive,
                                                    requester,
                                                    await (await requester.GetOrCreateDMChannelAsync())
                                                        .SendMessageAsync(
                                                            $"**{d.Source.Name}**: __{d.Id}__",
                                                            embed: MessageFormatter.EmbedDoujin(d)
                                                        ),
                                                    d,
                                                    _discord.Socket,
                                                    _json,
                                                    _settings
                                                );
                                            }
                                        )));

                                // delay
                                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                            }

                            _logger.LogDebug($"Sent update '{d.PrettyName ?? d.OriginalName}'");
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e,
                                $"Exception while sending update for doujin '{d.PrettyName ?? d.OriginalName}'");
                        }
                    }, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("No feed channels were found.");
                }

                _logger.LogDebug("Entering sleep.");

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.FeedUpdateInterval),
                    stoppingToken);

                _logger.LogDebug("Exited sleep.");
            }
        }

        static IEnumerable<string> TagsToChannels(IEnumerable<string> tags) =>
            tags.Select(t => t.ToLowerInvariant().Replace(' ', '-'));
    }
}