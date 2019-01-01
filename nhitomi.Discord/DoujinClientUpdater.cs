// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord.WebSocket;
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

        public DoujinClientUpdater(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            DiscordService discord,
            InteractiveScheduler interactive
        )
        {
            _settings = options.Value;
            _clients = clients;
            _discord = discord;
            _interactive = interactive;
        }

        readonly ConcurrentDictionary<IDoujinClient, IDoujin> _lastDoujins = new ConcurrentDictionary<IDoujinClient, IDoujin>();

        public async Task RunAsync(CancellationToken token)
        {
            // Alert channel
            var channel = _discord.Socket
                .GetGuild(_settings.Discord.Server.ServerId)
                .GetTextChannel(_settings.Discord.Server.AlertChannelId);

            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll(_clients.Select(async c =>
                {
                    // Update client
                    await c.UpdateAsync();

                    // Find new uploads
                    var doujins = (await c.SearchAsync(null)).GetEnumerator();

                    if (_lastDoujins.TryGetValue(c, out var lastDoujin))
                    {
                        var count = 0;

                        // Limit maximum alerts from each client to 20
                        while (count < 20 && await doujins.MoveNext(token))
                        {
                            var doujin = doujins.Current;

                            if (doujin.Id == lastDoujin.Id)
                                break;

                            if (count == 0)
                                _lastDoujins[c] = doujin;

                            // Create interactive
                            await _interactive.CreateInteractiveAsync(
                                requester: null,
                                response: await channel.SendMessageAsync(
                                    text: string.Empty,
                                    embed: MessageFormatter.EmbedDoujin(doujin)
                                ),
                                triggers: add => add(
                                    ("\uD83D\uDCBE", sendDownload)
                                ),
                                allowTrash: false
                            );

                            async Task sendDownload(SocketReaction reaction) =>
                                await DoujinModule.ShowDownload(
                                    doujin: doujin,
                                    channel: await _discord.Socket.GetUser(reaction.UserId).GetOrCreateDMChannelAsync(),
                                    settings: _settings);

                            count++;
                        }
                    }
                    else
                    {
                        await doujins.MoveNext(token);
                        _lastDoujins[c] = doujins.Current;
                    }
                }));

                // Sleep
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.Doujin.UpdateInterval),
                    token
                );
            }
        }
    }
}