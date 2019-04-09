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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Services;

namespace nhitomi
{
    public delegate Task<IUserMessage> SendMessageAsync(
        string text = null,
        bool isTTS = false,
        Embed embed = null,
        RequestOptions options = null);

    public class Interactive : IDisposable
    {
        public IUserMessage Message;

        public virtual void Dispose()
        {
        }
    }

    public abstract class ListInteractive : Interactive
    {
        public readonly IEnumerableBrowser Browser;

        protected ListInteractive(IEnumerableBrowser browser)
        {
            Browser = browser;
        }

        public abstract Embed CreateEmbed(MessageFormatter formatter);

        public virtual Task UpdateState(MessageFormatter formatter) =>
            Message.ModifyAsync(null, CreateEmbed(formatter));

        public override void Dispose() => Browser.Dispose();
    }

    public class DoujinListInteractive : ListInteractive
    {
        public IUserMessage DownloadMessage;

        public DoujinListInteractive(IAsyncEnumerable<IDoujin> doujins)
            : base(new EnumerableBrowser<IDoujin>(doujins.GetEnumerator()))
        {
        }

        IDoujin Current => ((IAsyncEnumerator<IDoujin>) Browser).Current;

        public override Embed CreateEmbed(MessageFormatter formatter) => formatter.CreateDoujinEmbed(Current);

        public override async Task UpdateState(MessageFormatter formatter)
        {
            await base.UpdateState(formatter);

            if (DownloadMessage != null)
                await DownloadMessage.ModifyAsync(embed: formatter.CreateDownloadEmbed(Current));
        }
    }

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

        async Task<bool> CreateListInteractiveAsync(
            ListInteractive interactive,
            SendMessageAsync sendMessage,
            CancellationToken cancellationToken = default)
        {
            // ensure list is not empty
            if (!await interactive.Browser.MoveNext(cancellationToken))
            {
                interactive.Dispose();
                await sendMessage(_formatter.EmptyList());

                return false;
            }

            // send interactive message
            interactive.Message = await sendMessage(embed: interactive.CreateEmbed(_formatter));

            // if list contains only one item, don't proceed to create the list
            if (!await interactive.Browser.MoveNext(cancellationToken))
            {
                interactive.Dispose();
                return true;
            }

            interactive.Browser.MovePrevious();

            // register interactive
            _listInteractives.AddOrUpdate(interactive.Message.Id, interactive, (a, b) => interactive);

            // schedule expiry
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.Discord.Command.InteractiveExpiry), default);

                if (_listInteractives.TryRemove(interactive.Message.Id, out var i))
                    await i.Message.DeleteAsync();
            }, default);

            // add paging triggers
            await _formatter.AddListTriggersAsync(interactive.Message);

            return true;
        }

        public async Task<DoujinListInteractive> CreateDoujinListInteractiveAsync(
            IAsyncEnumerable<IDoujin> doujins,
            SendMessageAsync sendMessage,
            CancellationToken cancellationToken = default)
        {
            var interactive = new DoujinListInteractive(doujins);

            return await CreateListInteractiveAsync(interactive, sendMessage, cancellationToken) ? interactive : null;
        }

        async Task HandleDoujinsDetected(IUserMessage message, IAsyncEnumerable<IDoujin> doujins)
        {
            var interactive = await CreateDoujinListInteractiveAsync(doujins, message.Channel.SendMessageAsync);

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
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

        async Task HandleReaction(SocketReaction reaction, bool added)
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
                    try
                    {
                        // destroy interactive if it is one
                        if (interactive != null &&
                            _listInteractives.TryRemove(message.Id, out _))
                            await interactive.Message.DeleteAsync();
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
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, $"Could not delete message {message.Id}");
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
                if (interactive.Browser.MovePrevious())
                    await interactive.UpdateState(_formatter);
                else
                    await interactive.Message.ModifyAsync(m => { m.Content = _formatter.BeginningOfList(); });

                return true;
            }

            // right arrow
            if (reaction.Emote.Equals(MessageFormatter.RightArrowEmote))
            {
                if (await interactive.Browser.MoveNext())
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

            IUserMessage downloadMessage;

            try
            {
                downloadMessage = await (await _discord.Socket.GetUser(reaction.UserId).GetOrCreateDMChannelAsync())
                    .SendMessageAsync(embed: _formatter.CreateDownloadEmbed(doujin));
            }
            catch
            {
                // user had disabled DMs
                downloadMessage = await reaction.Channel.SendMessageAsync(
                    embed: _formatter.CreateDownloadEmbed(doujin));
            }

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