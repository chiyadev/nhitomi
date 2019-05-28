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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nhitomi.Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class InteractiveManager : IReactionHandler
    {
        readonly IServiceProvider _services;
        readonly DiscordService _discord;
        readonly ILogger<InteractiveManager> _logger;

        public InteractiveManager(IServiceProvider services, DiscordService discord, ILogger<InteractiveManager> logger)
        {
            _services = services;
            _discord = discord;
            _logger = logger;
        }

        public readonly ConcurrentDictionary<ulong, IInteractiveMessage> InteractiveMessages =
            new ConcurrentDictionary<ulong, IInteractiveMessage>();

        public async Task SendInteractiveAsync(IEmbedMessage message, ICommandContext context,
            CancellationToken cancellationToken = default)
        {
            // create dependency scope to initialize the interactive within
            using (var scope = _services.CreateScope())
            {
                // initialize interactive
                if (!await message.UpdateViewAsync(scope.ServiceProvider, context, cancellationToken))
                    return;
            }

            // add to interactive list
            if (message is IInteractiveMessage interactiveMessage)
                InteractiveMessages[message.Message.Id] = interactiveMessage;
        }

        readonly ConcurrentQueue<(IUserMessage message, IEmote[] emotes)> _reactionQueue =
            new ConcurrentQueue<(IUserMessage message, IEmote[] emotes)>();

        public void EnqueueReactions(IUserMessage message, IEnumerable<IEmote> emotes)
        {
            _reactionQueue.Enqueue((message, emotes.ToArray()));

            _ = Task.Run(async () =>
            {
                while (_reactionQueue.TryDequeue(out var x))
                {
                    try
                    {
                        await x.message.AddReactionsAsync(x.emotes);
                    }
                    catch
                    {
                        // message may have been deleted
                        // or we don't have the perms
                    }
                }
            });
        }

        static readonly Dictionary<IEmote, Func<IReactionTrigger>> _statelessTriggers = typeof(Startup).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && typeof(IReactionTrigger).IsAssignableFrom(t))
            .Select(t => (Func<IReactionTrigger>) (() => Activator.CreateInstance(t) as IReactionTrigger))
            .Where(x => x().CanRunStateless)
            .ToDictionary(x => x().Emote, x => x);

        Task IReactionHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<bool> TryHandleAsync(ReactionContext context, CancellationToken cancellationToken = default)
        {
            var message = context.Message;
            var reaction = context.Reaction;

            try
            {
                IReactionTrigger trigger;

                // get interactive object for the message
                if (InteractiveMessages.TryGetValue(message.Id, out var interactive))
                {
                    // get trigger for this reaction
                    if (!interactive.Triggers.TryGetValue(reaction.Emote, out trigger))
                        return false;
                }
                else
                {
                    // no interactive; try triggering in stateless mode
                    if (!_statelessTriggers.TryGetValue(reaction.Emote, out var factory))
                        return false;

                    // message must be authored by us
                    if (!message.Reactions.TryGetValue(reaction.Emote, out var metadata) || !metadata.IsMe)
                        return false;

                    trigger = factory();
                }

                using (var scope = _services.CreateScope())
                    return await trigger.RunAsync(scope.ServiceProvider, context, interactive, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception while handling reaction {0} by for message {1}.",
                    reaction.Emote.Name, message.Id);

                await SendInteractiveAsync(new ErrorMessage(e), context, cancellationToken);
            }

            return true;
        }
    }
}