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
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Database;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class CollectionInteractive : ListInteractive
    {
        const int _itemsPerPage = 14;

        readonly string _collectionName;

        public CollectionInteractive(string collectionName, IEnumerable<CollectionItemInfo> items) : base(
            new EnumerableBrowser<IEnumerable<CollectionItemInfo>>(
                items.ChunkBy(_itemsPerPage).ToAsyncEnumerable().GetEnumerator()))
        {
            _collectionName = collectionName;
        }

        IEnumerable<CollectionItemInfo> Current =>
            ((IAsyncEnumerator<IEnumerable<CollectionItemInfo>>) Browser).Current;

        public override Embed CreateEmbed(MessageFormatter formatter) =>
            formatter.CreateCollectionEmbed(_collectionName, Current.ToArray());
    }

    public class InteractiveManager : IDisposable
    {
        readonly IServiceProvider _services;
        readonly AppSettings _settings;
        readonly DiscordService _discord;
        readonly MessageFormatter _formatter;
        readonly ILogger<InteractiveManager> _logger;

        public InteractiveManager(IServiceProvider services, IOptions<AppSettings> options, DiscordService discord,
            MessageFormatter formatter, ILogger<InteractiveManager> logger)
        {
            _services = services;
            _settings = options.Value;
            _discord = discord;
            _formatter = formatter;
            _discord = discord;
            _logger = logger;

            _discord.Socket.ReactionAdded += HandleReactionAsync;
            _discord.Socket.ReactionRemoved += HandleReactionAsync;

            _discord.DoujinsDetected += HandleDoujinsDetectedAsync;
        }

        public readonly ConcurrentDictionary<ulong, InteractiveMessage> InteractiveMessages =
            new ConcurrentDictionary<ulong, InteractiveMessage>();

        public async Task SendInteractiveAsync(InteractiveMessage interactive, ICommandContext context,
            CancellationToken cancellationToken = default)
        {
            // initialize interactive
            await interactive.InitializeAsync(_services, context, cancellationToken);

            // add to list
            InteractiveMessages[interactive.Message.Id] = interactive;
        }

        async Task HandleDoujinsDetectedAsync(IUserMessage message, IAsyncEnumerable<Doujin> doujins)
        {
            var interactive = await CreateDoujinListInteractiveAsync(doujins, message.Channel.SendMessageAsync);

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
        }

        static readonly Dictionary<IEmote, Func<ReactionTrigger>> _statelessTriggers = typeof(Startup).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && t.IsSubclassOf(typeof(ReactionTrigger)))
            .Select(t => (Func<ReactionTrigger>) (() => Activator.CreateInstance(t) as ReactionTrigger))
            .Where(x => x().CanRunStateless)
            .ToDictionary(x => x().Emote, x => x);

        Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Run(() => HandleReactionAsync(reaction));
            return Task.CompletedTask;
        }

        async Task HandleReactionAsync(SocketReaction reaction, CancellationToken cancellationToken = default)
        {
            try
            {
                // don't trigger reactions by us
                if (reaction.UserId == _discord.Socket.CurrentUser.Id)
                    return;

                ReactionTrigger trigger;

                // get interactive object for the message
                if (!InteractiveMessages.TryGetValue(reaction.MessageId, out var interactive))
                {
                    // no interactive; try triggering in stateless mode
                    if (!_statelessTriggers.TryGetValue(reaction.Emote, out var factory))
                        return;

                    // retrieve message
                    var message = reaction.Message.IsSpecified
                        ? reaction.Message.Value
                        : (IUserMessage) await reaction.Channel.GetMessageAsync(reaction.MessageId);

                    // message must be authored by us
                    if (message.Author.Id != _discord.Socket.CurrentUser.Id ||
                        !(message.Reactions.TryGetValue(reaction.Emote, out var metadata) && metadata.IsMe))
                        return;

                    trigger = factory();
                    trigger.Initialize(_services, null, message);

                    await trigger.RunAsync(cancellationToken);
                    return;
                }

                // get trigger for this reaction
                if (!interactive.Triggers.TryGetValue(reaction.Emote, out trigger))
                    return;

                await trigger.RunAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Exception while handling reaction {reaction.Emote.Name} by user {reaction.UserId}: {e.Message}");

                await reaction.Channel.SendMessageAsync(embed: _formatter.CreateErrorEmbed());
            }
        }

        public void Dispose()
        {
            _discord.Socket.ReactionAdded -= HandleReactionAsync;
            _discord.Socket.ReactionRemoved -= HandleReactionAsync;

            _discord.DoujinsDetected -= HandleDoujinsDetectedAsync;
        }
    }
}