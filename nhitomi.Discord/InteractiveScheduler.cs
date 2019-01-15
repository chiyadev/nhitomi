// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

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
    public class InteractiveScheduler
    {
        readonly AppSettings _settings;

        public InteractiveScheduler(
            IOptions<AppSettings> options
        )
        {
            _settings = options.Value;
        }

        readonly ConcurrentDictionary<ulong, Interactive> _interactives = new ConcurrentDictionary<ulong, Interactive>();
        public sealed class Interactive
        {
            public readonly ulong? RequesterId;
            public readonly ulong ResponseId;
            public readonly Dictionary<IEmote, Func<SocketReaction, Task>> Triggers = new Dictionary<IEmote, Func<SocketReaction, Task>>();

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
            var interactive = new Interactive(
                requester: requester?.Id,
                response: response.Id
            );

            // Interactive expiry time
            var expiryTime = DateTime.Now.AddMinutes(_settings.Discord.Command.InteractiveExpiry);

            // Add triggers
            if (triggers != null)
                triggers(collection =>
                {
                    foreach (var trigger in collection)
                        interactive.Triggers.Add(
                            key: new Emoji(trigger.emoji),
                            value: reaction =>
                            {
                                // Delay expiry on trigger
                                expiryTime = DateTime.Now.AddMinutes(_settings.Discord.Command.InteractiveExpiry);

                                return trigger.onTrigger(reaction);
                            }
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
                catch (TaskCanceledException) { }

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
                    key: new Emoji("\uD83D\uDDD1"),
                    value: reaction =>
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
            if (!_interactives.TryGetValue(reaction.MessageId, out var interactive) ||              // Message must be interactive
                !interactive.Triggers.TryGetValue(reaction.Emote, out var callback))                // Reaction must be a valid trigger
                return;

            // requester = reactor requirement
            // (interactive.RequesterId.HasValue && reaction.UserId != interactive.RequesterId) || // Reaction must be by the original requester

            // Execute callback
            await callback(reaction);
        }
    }
}