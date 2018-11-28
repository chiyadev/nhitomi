using Discord;
using Discord.Commands;
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
    public class InteractiveScheduler : IDisposable
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;

        public InteractiveScheduler(
            IOptions<AppSettings> options,
            DiscordService discord
        )
        {
            _settings = options.Value;
            _discord = discord;
            _discord.Socket.ReactionAdded += handleReaction;
            _discord.Socket.ReactionRemoved += handleReaction;
        }

        readonly ConcurrentDictionary<ulong, Interactive> _interactives = new ConcurrentDictionary<ulong, Interactive>();
        public sealed class Interactive
        {
            public readonly ulong RequesterId;
            public readonly ulong ResponseId;
            public readonly Dictionary<IEmote, Func<Task>> Triggers = new Dictionary<IEmote, Func<Task>>();

            public Interactive(
                ulong requester,
                ulong response
            )
            {
                RequesterId = requester;
                ResponseId = response;
            }
        }

        public delegate void AddTriggers(params (string emoji, Func<Task> onTrigger)[] triggers);

        public async Task CreateInteractiveAsync(
            ICommandContext context,
            IUserMessage response,
            Action<AddTriggers> triggers,
            Func<Task> onExpire = null,
            bool allowTrash = false
        )
        {
            // Create interactive
            var interactive = new Interactive(
                requester: context.User.Id,
                response: response.Id
            );

            // Add triggers
            triggers(collection =>
            {
                foreach (var trigger in collection)
                    interactive.Triggers.Add(
                        key: new Emoji(trigger.emoji),
                        value: trigger.onTrigger
                    );
            });

            // Register interactive
            _interactives.AddOrUpdate(
                key: interactive.ResponseId,
                addValue: interactive,
                updateValueFactory: delegate { return interactive; }
            );

            // Schedule expiry
            var expiryDelayToken = new CancellationTokenSource();
            var expiryDelete = false;
            var expiryTask = expire();

            async Task expire()
            {
                try
                {
                    // Wait until expiry
                    await Task.Delay(
                        TimeSpan.FromMinutes(_settings.Discord.Command.InteractiveExpiry),
                        expiryDelayToken.Token
                    );
                }
                catch (TaskCanceledException) { }

                // Unregister interactive
                _interactives.TryRemove(interactive.ResponseId, out _);

                // Delete interactive
                if (expiryDelete)
                    await response.DeleteAsync();

                // Expiry event
                if (onExpire != null)
                    await onExpire();

                expiryDelayToken.Dispose();
            }

            if (allowTrash)
                interactive.Triggers.Add(
                    key: new Emoji("\u274e"),
                    value: () =>
                    {
                        expiryDelete = true;
                        expiryDelayToken.Cancel();
                        return expiryTask;
                    }
                );

            // Add trigger reactions
            foreach (var trigger in interactive.Triggers.Keys)
                await response.AddReactionAsync(trigger);
        }

        async Task handleReaction(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!_interactives.TryGetValue(reaction.MessageId, out var interactive) ||  // Message must be interactive
                reaction.UserId != interactive.RequesterId ||                           // Reaction must be by the original requester
                !interactive.Triggers.TryGetValue(reaction.Emote, out var callback))    // Reaction must be a valid trigger
                return;

            // Execute callback
            await callback();
        }

        public void Dispose()
        {
            _discord.Socket.ReactionAdded -= handleReaction;
            _discord.Socket.ReactionRemoved -= handleReaction;
        }
    }
}