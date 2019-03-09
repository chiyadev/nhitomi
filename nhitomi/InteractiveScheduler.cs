// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class InteractiveScheduler
    {
        readonly AppSettings _settings;

        public ICollection<ulong> IgnoreReactionUsers { get; } = new HashSet<ulong>();

        public InteractiveScheduler(
            IOptions<AppSettings> options
        )
        {
            _settings = options.Value;
        }

        readonly ConcurrentDictionary<ulong, Interactive>
            _interactives = new ConcurrentDictionary<ulong, Interactive>();

        public sealed class Interactive
        {
            public readonly ulong? RequesterId;
            public readonly ulong ResponseId;

            public readonly Dictionary<IEmote, Func<SocketReaction, Task>> Triggers =
                new Dictionary<IEmote, Func<SocketReaction, Task>>();

            public Interactive(
                ulong? requester,
                ulong response
            )
            {
                RequesterId = requester;
                ResponseId = response;
            }
        }

        public delegate void AddTriggers(params (string emoji, Func<SocketReaction, Task> onTrigger)[] triggers);

        public async Task CreateInteractiveAsync(
            IUser requester,
            IUserMessage response,
            Action<AddTriggers> triggers = null,
            Func<Task> onExpire = null,
            bool allowTrash = false
        )
        {
            // Create interactive
            var interactive = new Interactive(requester?.Id, response.Id);

            // Interactive expiry time
            var expiryTime = DateTime.Now.AddMinutes(_settings.Discord.Command.InteractiveExpiry);

            // Add triggers
            if (triggers != null)
                triggers(collection =>
                {
                    foreach (var trigger in collection)
                        interactive.Triggers.Add(
                            new Emoji(trigger.emoji),
                            reaction =>
                            {
                                // Delay expiry on trigger
                                expiryTime = DateTime.Now.AddMinutes(_settings.Discord.Command.InteractiveExpiry);

                                return trigger.onTrigger(reaction);
                            }
                        );
                });

            // Register interactive
            _interactives.AddOrUpdate(interactive.ResponseId, interactive, (_, _) => interactive);

            // Schedule expiry
            var expiryDelayToken = new CancellationTokenSource();
            var expireDelete = false;
            var expiryTask = expire();

            async Task expire()
            {
                try
                {
                    // Wait until expiry
                    while (expiryTime > DateTime.Now)
                    {
                        await Task.Delay(
                            expiryTime - DateTime.Now,
                            expiryDelayToken.Token
                        );
                    }
                }
                catch (TaskCanceledException)
                {
                }

                // Unregister interactive
                _interactives.TryRemove(interactive.ResponseId, out _);

                // Delete interactive
                if (expireDelete)
                    await response.DeleteAsync();

                // Expiry event
                if (onExpire != null)
                {
                    var task = onExpire();
                    if (task != null)
                        await task;
                }

                expiryDelayToken.Dispose();
            }

            if (allowTrash)
                interactive.Triggers.Add(
                    new Emoji("\uD83D\uDDD1"),
                    reaction =>
                    {
                        expireDelete = true;
                        expiryDelayToken.Cancel();
                        return expiryTask;
                    }
                );

            // Add trigger reactions
            foreach (var trigger in interactive.Triggers.Keys)
                await response.AddReactionAsync(trigger);
        }

        public async Task HandleReaction(
            Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            if (!_interactives.TryGetValue(reaction.MessageId, out var interactive) || // Message must be interactive
                IgnoreReactionUsers.Contains(reaction.UserId) || // Reaction user must not be ignoring
                !interactive.Triggers.TryGetValue(reaction.Emote, out var callback)) // Reaction must be a valid trigger
                return;

            // Execute callback
            await callback(reaction);
        }
    }
}
