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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Services;

namespace nhitomi
{
    public class InteractiveManager
    {
        readonly AppSettings _settings;
        DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly ISet<IDoujinClient> _clients;
        readonly ILogger<InteractiveManager> _logger;

        // DiscordService will assign this to itself
        // this is a workaround for circular dependency DiscordService -> InteractiveManager -> DiscordService ...
        internal DiscordService DiscordService
        {
            set => _discord = value;
        }

        public InteractiveManager(
            IOptions<AppSettings> options,
            MessageFormatter formatter,
            ISet<IDoujinClient> clients,
            ILogger<InteractiveManager> logger)
        {
            _settings = options.Value;
            _formatter = formatter;
            _logger = logger;
            _clients = clients;
        }

        readonly ConcurrentDictionary<ulong, ListInteractive>
            _listInteractives = new ConcurrentDictionary<ulong, ListInteractive>();

        sealed class ListInteractive
        {
            public readonly IUserMessage Message;
            public readonly EnumerableBrowser<Embed> Browser;

            public ListInteractive(
                IUserMessage message,
                EnumerableBrowser<Embed> browser)
            {
                Message = message;
                Browser = browser;
            }
        }

        public async Task<bool> InitListInteractiveAsync(
            IUserMessage message,
            IAsyncEnumerable<Embed> embeds,
            CancellationToken cancellationToken = default)
        {
            var browser = new EnumerableBrowser<Embed>(embeds.GetEnumerator());

            // ensure list is not empty
            if (!await browser.MoveNext(cancellationToken))
            {
                browser.Dispose();
                await message.ModifyAsync(_formatter.EmptyList());

                return false;
            }

            // if list contains only one item, don't proceed to create the list
            if (!await browser.MoveNext(cancellationToken))
            {
                browser.Dispose();
                return true;
            }

            browser.MovePrevious();

            var interactive = new ListInteractive(message, browser);
            var key = message.Id;

            // register interactive
            _listInteractives.AddOrUpdate(key, interactive, (a, b) => interactive);

            // schedule expiry
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.Discord.Command.InteractiveExpiry), default);

                _listInteractives.TryRemove(key, out _);
            }, default);

            // show first item
            await message.ModifyAsync(embed: browser.Current);

            // add paging triggers
            await _formatter.AddListTriggersAsync(message);

            return true;
        }

        public async Task HandleReaction(SocketReaction reaction, bool attached)
        {
            // don't trigger reactions ourselves
            if (reaction.UserId == _discord.Socket.CurrentUser.Id)
                return;

            // get interactive message
            IUserMessage message;

            if (reaction.Message.IsSpecified)
                message = reaction.Message.Value;
            else if (await reaction.Channel.GetMessageAsync(reaction.MessageId) is IUserMessage m)
                message = m;
            else
                return;

            // interactive must be authored by us
            if (message.Author.Id != _discord.Socket.CurrentUser.Id)
                return;

            try
            {
                // list interactive handling
                if (_listInteractives.TryGetValue(message.Id, out var listInteractive) &&
                    await HandleListInteractiveReaction(reaction, listInteractive))
                    return;

                if (!attached)
                    return;

                // delete trigger
                if (reaction.Emote.Equals(MessageFormatter.TrashcanEmote))
                {
                    await message.DeleteAsync();

                    // unregister list interactive if it is one
                    if (listInteractive != null)
                        _listInteractives.TryRemove(message.Id, out _);

                    return;
                }

                // download trigger
                if (reaction.Emote.Equals(MessageFormatter.FloppyDiskEmote) &&
                    await HandleDoujinDownloadReaction(reaction, message))
                    return;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Exception while handling reaction {reaction.Emote.Name} by user {reaction.UserId}: {e.Message}");

                await reaction.Channel.SendMessageAsync(embed: _formatter.CreateErrorEmbed());
            }
        }

        async Task<bool> HandleListInteractiveReaction(IReaction reaction, ListInteractive interactive)
        {
            // left arrow
            if (reaction.Emote.Equals(MessageFormatter.LeftArrowEmote))
            {
                if (interactive.Browser.MovePrevious())
                    await interactive.Message.ModifyAsync(embed: interactive.Browser.Current);
                else
                    await interactive.Message.ModifyAsync(m => { m.Content = _formatter.BeginningOfList(); });

                return true;
            }

            // right arrow
            if (reaction.Emote.Equals(MessageFormatter.RightArrowEmote))
            {
                if (await interactive.Browser.MoveNext(default))
                    await interactive.Message.ModifyAsync(embed: interactive.Browser.Current);
                else
                    await interactive.Message.ModifyAsync(m => { m.Content = _formatter.EndOfList(); });

                return false;
            }

            return false;
        }

        async Task<bool> HandleDoujinDownloadReaction(IReaction reaction, IUserMessage message)
        {
            // source/id
            var identifier = message.Embeds
                .FirstOrDefault(e => e is Embed)?.Fields
                .FirstOrDefault(f => f.Name == "ID").Value;

            if (identifier == null)
                return false;

            identifier.Split('/', 2).Destructure(out var source, out var id);

            var client = _clients.FindByName(source);
            if (client == null)
                return false;

            var doujin = await client.GetAsync(id);
            if (doujin == null)
                return false;

            var downloadMessage = await (await message.Author.GetOrCreateDMChannelAsync())
                .SendMessageAsync(embed: _formatter.CreateDownloadEmbed(doujin));

            await _formatter.AddDownloadTriggersAsync(downloadMessage);

            return true;
        }
    }
}
