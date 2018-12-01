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
            IUser requester,
            IUserMessage response,
            Action<AddTriggers> triggers = null,
            Func<Task> onExpire = null,
            bool allowTrash = false
        )
        {
            // Create interactive
            var interactive = new Interactive(
                requester: requester.Id,
                response: response.Id
            );

            // Add triggers
            if (triggers != null)
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

        public async Task HandleReaction(
            Cacheable<IUserMessage, ulong> cacheable,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            if (!_interactives.TryGetValue(reaction.MessageId, out var interactive) ||  // Message must be interactive
                reaction.UserId != interactive.RequesterId ||                           // Reaction must be by the original requester
                !interactive.Triggers.TryGetValue(reaction.Emote, out var callback))    // Reaction must be a valid trigger
                return;

            // Execute callback
            await callback();
        }
    }
}