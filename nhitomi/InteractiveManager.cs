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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Services;

namespace nhitomi
{
    public class InteractiveManager : IDisposable
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly ISet<IDoujinClient> _clients;
        readonly ILogger<InteractiveManager> _logger;

        public InteractiveManager(
            IOptions<AppSettings> options,
            DiscordService discord,
            MessageFormatter formatter,
            ISet<IDoujinClient> clients,
            ILogger<InteractiveManager> logger)
        {
            _settings = options.Value;
            _discord = discord;
            _formatter = formatter;
            _logger = logger;
            _discord = discord;
            _clients = clients;

            _discord.Socket.ReactionAdded += HandleReactionAddedAsyncBackground;
            _discord.Socket.ReactionRemoved += HandleReactionRemovedAsyncBackground;

            _discord.DoujinsDetected += HandleDoujinsDetected;
        }

        readonly ConcurrentDictionary<ulong, ListInteractive>
            _listInteractives = new ConcurrentDictionary<ulong, ListInteractive>();

        class Interactive
        {
            public readonly IUserMessage Message;

            public Interactive(IUserMessage message)
            {
                Message = message;
            }

            public virtual Task DestroyAsync() => Message.DeleteAsync();
        }

        abstract class ListInteractive : Interactive, IDisposable
        {
            public ListInteractive(
                IUserMessage message)
                : base(message)
            {
            }

            public abstract Task<bool> MoveNext(CancellationToken cancellationToken = default);
            public abstract bool MovePrevious();
            public abstract Task UpdateState(MessageFormatter formatter);

            public virtual void Dispose()
            {
            }
        }

        class DoujinListInteractive : ListInteractive
        {
            readonly EnumerableBrowser<IDoujin> _browser;

            public IUserMessage DownloadMessage;

            public DoujinListInteractive(
                IUserMessage message,
                EnumerableBrowser<IDoujin> browser)
                : base(message)
            {
                _browser = browser;
            }

            public override Task<bool> MoveNext(CancellationToken cancellationToken = default) =>
                _browser.MoveNext(cancellationToken);

            public override bool MovePrevious() => _browser.MovePrevious();

            public override async Task UpdateState(MessageFormatter formatter)
            {
                await Message.ModifyAsync(embed: formatter.CreateDoujinEmbed(_browser.Current));

                if (DownloadMessage != null)
                    await DownloadMessage.ModifyAsync(embed: formatter.CreateDownloadEmbed(_browser.Current));
            }

            public override async Task DestroyAsync()
            {
                if (DownloadMessage != null)
                    await DownloadMessage.DeleteAsync();

                await base.DestroyAsync();
            }

            public override void Dispose()
            {
                base.Dispose();
                _browser.Dispose();
            }
        }

        public Task<bool> CreateDoujinListInteractiveAsync(
            IUserMessage message,
            IAsyncEnumerable<IDoujin> doujins,
            CancellationToken cancellationToken = default) =>
            InitListInteractiveAsync(
                new DoujinListInteractive(message, new EnumerableBrowser<IDoujin>(doujins.GetEnumerator())),
                cancellationToken);

        async Task<bool> InitListInteractiveAsync(
            ListInteractive interactive,
            CancellationToken cancellationToken = default)
        {
            // ensure list is not empty
            if (!await interactive.MoveNext(cancellationToken))
            {
                interactive.Dispose();
                await interactive.Message.ModifyAsync(_formatter.EmptyList());

                return false;
            }

            // show first item
            await interactive.UpdateState(_formatter);

            // if list contains only one item, don't proceed to create the list
            if (!await interactive.MoveNext(cancellationToken))
            {
                interactive.Dispose();
                return true;
            }

            interactive.MovePrevious();

            // register interactive
            _listInteractives.AddOrUpdate(interactive.Message.Id, interactive, (a, b) => interactive);

            // schedule expiry
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.Discord.Command.InteractiveExpiry), default);

                if (_listInteractives.TryRemove(interactive.Message.Id, out var i))
                    await i.DestroyAsync();
            }, default);

            // add paging triggers
            await _formatter.AddListTriggersAsync(interactive.Message);

            return true;
        }

        async Task HandleDoujinsDetected(IUserMessage message, IUserMessage response, IAsyncEnumerable<IDoujin> doujins)
        {
            if (await CreateDoujinListInteractiveAsync(response, doujins))
                await _formatter.AddDoujinTriggersAsync(response);
        }

        Task HandleReactionAddedAsyncBackground(
            Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Run(() => HandleReaction(reaction, true));
            return Task.CompletedTask;
        }

        Task HandleReactionRemovedAsyncBackground(
            Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Run(() => HandleReaction(reaction, false));
            return Task.CompletedTask;
        }

        public async Task HandleReaction(SocketReaction reaction, bool added)
        {
            // don't trigger reactions ourselves
            if (reaction.UserId == _discord.Socket.CurrentUser.Id)
                return;

            // get interactive message
            if (!(await reaction.Channel.GetMessageAsync(reaction.MessageId) is IUserMessage message))
                return;

            // interactive must be authored by us
            if (message.Author.Id != _discord.Socket.CurrentUser.Id)
                return;

            try
            {
                // list interactive handling
                if (_listInteractives.TryGetValue(message.Id, out var interactive) &&
                    await HandleListInteractiveReaction(reaction, interactive))
                    return;

                // delete trigger
                if (reaction.Emote.Equals(MessageFormatter.TrashcanEmote))
                {
                    // destroy interactive if it is one
                    if (interactive != null &&
                        _listInteractives.TryRemove(message.Id, out _))
                        await interactive.DestroyAsync();
                    else
                        await message.DeleteAsync();

                    foreach (var i in _listInteractives.Values.OfType<DoujinListInteractive>())
                        if (i.DownloadMessage?.Id == message.Id)
                        {
                            i.DownloadMessage = null;
                            break;
                        }

                    return;
                }

                // download trigger
                if (reaction.Emote.Equals(MessageFormatter.FloppyDiskEmote) &&
                    await HandleDoujinDownloadReaction(reaction, message, interactive))
                    return;

                // favourite trigger
                if (added &&
                    reaction.Emote.Equals(MessageFormatter.HeartEmote) &&
                    await HandleDoujinFavoriteReaction(reaction, message))
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
                if (interactive.MovePrevious())
                    await interactive.UpdateState(_formatter);
                else
                    await interactive.Message.ModifyAsync(m => { m.Content = _formatter.BeginningOfList(); });

                return true;
            }

            // right arrow
            if (reaction.Emote.Equals(MessageFormatter.RightArrowEmote))
            {
                if (await interactive.MoveNext())
                    await interactive.UpdateState(_formatter);
                else
                    await interactive.Message.ModifyAsync(m => { m.Content = _formatter.EndOfList(); });

                return false;
            }

            return false;
        }

        async Task<IDoujin> GetDoujinFromMessage(IMessage message)
        {
            // source/id
            var identifier = message.Embeds.FirstOrDefault(e => e is Embed)?.Footer?.Text;

            if (identifier == null)
                return null;

            identifier.Split('/', 2).Destructure(out var source, out var id);

            var client = _clients.FindByName(source);
            if (client == null)
                return null;

            return await client.GetAsync(id);
        }

        async Task<bool> HandleDoujinDownloadReaction(
            SocketReaction reaction,
            IMessage message,
            Interactive interactive)
        {
            var doujin = await GetDoujinFromMessage(message);
            if (doujin == null)
                return false;

            var downloadMessage = await (await _discord.Socket.GetUser(reaction.UserId).GetOrCreateDMChannelAsync())
                .SendMessageAsync(embed: _formatter.CreateDownloadEmbed(doujin));

            if (interactive is DoujinListInteractive doujinListInteractive)
                doujinListInteractive.DownloadMessage = downloadMessage;

            await _formatter.AddDownloadTriggersAsync(downloadMessage);

            return true;
        }

        async Task<bool> HandleDoujinFavoriteReaction(SocketReaction reaction, IMessage interactive)
        {
            var doujin = await GetDoujinFromMessage(interactive);
            if (doujin == null)
                return false;

            var doujinMessage = await (await _discord.Socket.GetUser(reaction.UserId).GetOrCreateDMChannelAsync())
                .SendMessageAsync(embed: _formatter.CreateDoujinEmbed(doujin));

            await _formatter.AddDoujinTriggersAsync(doujinMessage);

            return true;
        }

        public void Dispose()
        {
            _discord.Socket.ReactionAdded -= HandleReactionAddedAsyncBackground;
            _discord.Socket.ReactionRemoved -= HandleReactionRemovedAsyncBackground;

            _discord.DoujinsDetected -= HandleDoujinsDetected;
        }
    }
}
