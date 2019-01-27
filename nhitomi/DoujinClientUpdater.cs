// Copyright (c) 2019 phosphene47
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

namespace nhitomi
{
    public class DoujinClientUpdater : BackgroundService
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly DiscordService _discord;
        readonly InteractiveScheduler _interactive;
        readonly ILogger _logger;

        public DoujinClientUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            InteractiveScheduler interactive,
            ILogger<DoujinClientUpdater> logger
        )
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _interactive = interactive;
            _logger = logger;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins = new ConcurrentDictionary<IDoujinClient, IDoujin>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _discord.EnsureConnectedAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Concurrently update clients
                await Task.WhenAll(_clients.Select(async c =>
                {
                    try { await c.UpdateAsync(); }
                    catch (Exception e) { _logger.LogWarning(e, $"Exception while updating client '{c.Name}': {e.Message}"); }
                }));

                // Concurrently find recent uploads
                var newDoujins = AsyncEnumerable.Concat(await Task.WhenAll(_clients.Select(async c =>
                {
                    try
                    {
                        var list = await c.SearchAsync(null);

                        if (!_lastDoujins.ContainsKey(c))
                        {
                            // Handling first time update
                            var doujin = await list.FirstOrDefault();

                            if (doujin != null)
                                _lastDoujins[c] = doujin;
                        }
                        else
                        {
                            // Get all new doujins up to the last one we know
                            list = list.TakeWhile(d => d.Id != _lastDoujins[c].Id);

                            _lastDoujins[c] = await list.FirstOrDefault() ?? _lastDoujins[c];

                            return list;
                        }
                    }
                    catch (Exception e) { _logger.LogWarning(e, $"Exception while searching client '{c.Name}': {e.Message}"); }

                    return AsyncEnumerable.Empty<IDoujin>();
                })));

                // Get feed channels
                var channels =
                    (_discord.Socket.GetChannel(_settings.Discord.Server.FeedCategoryId) as SocketCategoryChannel).Channels
                    .OfType<ITextChannel>()
                    .ToArray();

                if (channels != null)
                {
                    _logger.LogDebug($"Found {channels.Length} feed channels: {string.Join(", ", channels.Select(c => c.Name))}");

                    // Concurrently send new updates
                    await Task.WhenAll(await newDoujins
                        .SelectMany(d => channels
                            .Where(c => tagsToChannels(d.Tags).Contains(c.Name))
                            .Select(async c =>
                            {
                                await _interactive.CreateInteractiveAsync(
                                    requester: null,
                                    response: await c.SendMessageAsync(
                                        text: $"**{c.Name}**: __{d.Id}__",
                                        embed: MessageFormatter.EmbedDoujin(d)
                                    ),
                                    triggers: add => add(
                                        ("\u2764", async r =>
                                        {
                                            var requester = _discord.Socket.GetUser(r.UserId);

                                            await DoujinModule.ShowDoujin(
                                                interactive: _interactive,
                                                requester: requester,
                                                response: await (await requester.GetOrCreateDMChannelAsync()).SendMessageAsync(
                                                    text: $"**{c.Name}**: __{d.Id}__",
                                                    embed: MessageFormatter.EmbedDoujin(d)
                                                ),
                                                doujin: d,
                                                settings: _settings
                                            );
                                        }
                                    )
                                    ),
                                    allowTrash: false
                                );
                            })
                            .ToAsyncEnumerable())
                        .ToArray());
                }
                else
                    _logger.LogDebug($"No feed channels were found.");

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.UpdateInterval),
                    stoppingToken
                );
            }
        }

        static IEnumerable<string> tagsToChannels(IEnumerable<string> tags) =>
            tags.Select(t =>
            {
                var tag = t.ToLowerInvariant().Replace(' ', '-');

                switch (tag)
                {
                    default:
                        return tag;
                    case "loli":
                        return "lolicon";
                }
            });
    }
}