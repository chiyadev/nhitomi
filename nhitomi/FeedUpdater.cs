// Copyright (c) 2018-2019 phosphene47
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
        readonly ILogger _logger;

        public FeedUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            InteractiveScheduler interactive,
            JsonSerializer json,
            ILogger<FeedUpdater> logger
        )
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
                _logger.LogDebug("Starting feed update.");

                // Concurrently update clients
                await Task.WhenAll(_clients.Select(async c =>
                {
                    try
                    {
                        await c.UpdateAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Exception while updating client '{c.Name}': {e.Message}");
                    }
                }));

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
                            (await c.SearchAsync(null))
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

                _logger.LogDebug("Finding feed channels.");

                // Get feed channels
                var channels =
                    (_discord.Socket.GetChannel(529830517781037056) as SocketCategoryChannel)?.Channels
                    .OfType<ITextChannel>()
                    .ToArray();

                if (channels != null && channels.Length != 0)
                {
                    _logger.LogDebug(
                        $"Found {channels.Length} feed channels: {string.Join(", ", channels.Select(c => c.Name))}");

                    // Concurrently send new updates
                    await Task.WhenAll(await newDoujins
                        .SelectMany(d => channels
                            .Where(c => tagsToChannels(d.Tags).Contains(c.Name))
                            .Select(async c =>
                            {
                                await _interactive.CreateInteractiveAsync(
                                    null,
                                    await c.SendMessageAsync(
                                        $"**{d.Source.Name}**: __{d.Id}__",
                                        embed: MessageFormatter.EmbedDoujin(d)
                                    ),
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
                                        ))
                                );
                            })
                            .ToAsyncEnumerable())
                        .ToArray(stoppingToken));

                    _logger.LogDebug("Sent feed updates.");
                }
                else
                {
                    _logger.LogDebug($"No feed channels were found.");
                }

                _logger.LogDebug("Entering sleep.");

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.FeedUpdateInterval),
                    stoppingToken);

                _logger.LogDebug("Exited sleep.");
            }
        }

        static IEnumerable<string> tagsToChannels(IEnumerable<string> tags) =>
            tags.Select(t => t.ToLowerInvariant().Replace(' ', '-'));
    }
}