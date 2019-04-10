using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Database;

namespace nhitomi.Modules
{
    [Group("subscription")]
    [Alias("sub")]
    public class SubscriptionModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;

        public SubscriptionModule(
            IDatabase database,
            MessageFormatter formatter)
        {
            _database = database;
            _formatter = formatter;
        }

        [Command]
        public async Task ListSubscriptionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var tags = await _database.GetTagSubscriptionsAsync(Context.User.Id);

                await ReplyAsync(embed: _formatter.CreateSubscriptionListEmbed(tags));
            }
        }

        [Command("add")]
        public async Task SubscribeAsync([Remainder] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            using (Context.Channel.EnterTypingState())
            {
                if (await _database.TryAddTagSubscriptionAsync(Context.User.Id, tag))
                    await ReplyAsync(_formatter.AddedSubscription(tag));
                else
                    await ReplyAsync(_formatter.AlreadySubscribed(tag));
            }
        }

        [Command("remove")]
        public async Task UnsubscribeAsync([Remainder] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            using (Context.Channel.EnterTypingState())
            {
                if (await _database.TryRemoveTagSubscriptionAsync(Context.User.Id, tag))
                    await ReplyAsync(_formatter.RemovedSubscription(tag));
                else
                    await ReplyAsync(_formatter.NotSubscribed(tag));
            }
        }

        [Command("enable")]
        public async Task EnableNotificationsAsync()
        {
            // todo:
        }

        [Command("disable")]
        public async Task DisableNotificationsAsync()
        {
            // todo:
        }

        [Command("clear")]
        public async Task ClearAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                await _database.ClearTagSubscriptionsAsync(Context.User.Id);

                await ReplyAsync(_formatter.ClearedSubscriptions());
            }
        }
    }
}