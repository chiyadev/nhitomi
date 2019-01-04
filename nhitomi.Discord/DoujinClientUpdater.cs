// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Discord.WebSocket;
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
    public class DoujinClientUpdater : IBackgroundService
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

        public async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Get feed channels
                var channels = _discord.Socket
                    .GetGuild(_settings.Discord.Server.ServerId).CategoryChannels
                    .FirstOrDefault(c => c.Id == _settings.Discord.Server.FeedCategoryId)?.Channels
                    .ToDictionary(c => c.Name.ToLowerInvariant(), c => c as ITextChannel);

                if (channels != null)
                    _logger.LogDebug($"Found {channels.Count} feed channels: {string.Join(", ", channels.Select(c => c.Key))}");
                else
                    _logger.LogDebug($"No feed channels were found.");

                await Task.WhenAll(_clients.Select(async c =>
                {
                    try
                    {
                        await updateClient(c, channels, token);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Exception while updating client '{c.Name}': {e.Message}.");
                    }
                }));

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.UpdateInterval),
                    token
                );
            }
        }

        async Task updateClient(IDoujinClient client, IReadOnlyDictionary<string, ITextChannel> channels, CancellationToken token)
        {
            // Update client
            await client.UpdateAsync();

            if (channels == null)
                return;

            // Find new uploads
            using (var doujins = (await client.SearchAsync(null)).GetEnumerator())
            {
                var count = 0;

                if (_lastDoujins.TryGetValue(client, out var lastDoujin))
                {
                    // Limit maximum alerts from each client to 20
                    while (count < 20 && await doujins.MoveNext(token))
                    {
                        var doujin = doujins.Current;

                        if (doujin.Id == lastDoujin.Id)
                            break;

                        if (count == 0)
                            _lastDoujins[client] = doujin;

                        // Create interactive
                        foreach (var channel in channels.Where(c => doujin.Tags.Contains(c.Key)).Select(c => c.Value))
                        {
                            await _interactive.CreateInteractiveAsync(
                                requester: null,
                                response: await channel.SendMessageAsync(
                                    text: string.Empty,
                                    embed: MessageFormatter.EmbedDoujin(doujin)
                                ),
                                triggers: add => add(
                                    ("\u2764\uFE0F", sendDoujin)
                                ),
                                allowTrash: false
                            );
                        }

                        async Task sendDoujin(SocketReaction reaction)
                        {
                            var requester = _discord.Socket.GetUser(reaction.UserId);

                            await DoujinModule.ShowDoujin(
                                interactive: _interactive,
                                requester: requester,
                                response: await (await requester.GetOrCreateDMChannelAsync()).SendMessageAsync(
                                    text: string.Empty,
                                    embed: MessageFormatter.EmbedDoujin(doujin)
                                ),
                                doujin: doujin,
                                settings: _settings
                            );
                        }

                        count++;
                    }
                }
                else
                {
                    await doujins.MoveNext(token);

                    if (doujins.Current != null)
                        _lastDoujins[client] = doujins.Current;
                }

                _logger.LogDebug($"Updated client '{client.Name}', alerted {count} doujins.");
            }
        }
    }
}