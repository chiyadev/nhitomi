using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class InteractiveManager : IMessageHandler, IReactionHandler
    {
        readonly IServiceProvider _services;

        public InteractiveManager(IServiceProvider services)
        {
            _services = services;
        }

        public readonly ConcurrentDictionary<ulong, IInteractiveMessage> InteractiveMessages =
            new ConcurrentDictionary<ulong, IInteractiveMessage>();

        public async Task SendInteractiveAsync(IEmbedMessage message, IDiscordContext context,
            CancellationToken cancellationToken = default, bool forceStateful = true)
        {
            // create dependency scope to initialize the interactive within
            using (var scope = _services.CreateScope())
            {
                var services = new ServiceDictionary(scope.ServiceProvider)
                {
                    {typeof(IDiscordContext), context}
                };

                // initialize interactive
                if (!await message.UpdateViewAsync(services, cancellationToken))
                    return;
            }

            var id = message.Message.Id;

            if (message is IInteractiveMessage interactiveMessage)
            {
                // add to interactive list if we have stateful triggers
                if (forceStateful || interactiveMessage.Triggers.Values.Any(t => !t.CanRunStateless))
                    InteractiveMessages[id] = interactiveMessage;
            }

            // forget interactives in an hour
            _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    finally
                    {
                        InteractiveMessages.TryRemove(id, out _);
                    }
                },
                cancellationToken);
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
            .Where(t => !t.IsAbstract && t.IsClass &&
                        typeof(IReactionTrigger).IsAssignableFrom(t) &&
                        t.GetConstructors().Any(c => c.GetParameters().Length == 0))
            .Select(t => (Func<IReactionTrigger>) (() => Activator.CreateInstance(t) as IReactionTrigger))
            .Where(x => x().CanRunStateless)
            .ToDictionary(x => x().Emote, x => x);

        Task IMessageHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        Task IReactionHandler.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> TryHandleAsync(IMessageContext context, CancellationToken cancellationToken = default)
        {
            switch (context.Event)
            {
                // remove from interactive list
                case MessageEvent.Delete when InteractiveMessages.TryRemove(context.Message.Id, out var interactive):

                    // dispose interactive object
                    interactive.Dispose();

                    return Task.FromResult(true);

                default:
                    return Task.FromResult(false);
            }
        }

        public async Task<bool> TryHandleAsync(IReactionContext context, CancellationToken cancellationToken = default)
        {
            var message = context.Message;
            var reaction = context.Reaction;

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

            // dependency scope
            using (var scope = _services.CreateScope())
            {
                var services = new ServiceDictionary(scope.ServiceProvider)
                {
                    {typeof(IDiscordContext), context},
                    {typeof(IReactionContext), context}
                };

                return await trigger.RunAsync(services, interactive, cancellationToken);
            }
        }
    }
}