using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Database;

namespace nhitomi.Modules
{
    [Group("subscriptions")]
    [Alias("subs")]
    public class SubscriptionsModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;

        public SubscriptionsModule(IDatabase database, MessageFormatter formatter)
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
                await _database.AddTagSubscriptionAsync(Context.User.Id, tag);

                await ReplyAsync(_formatter.SubscribeSuccess(tag));
            }
        }

        [Command("remove")]
        public async Task UnsubscribeAsync([Remainder] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            using (Context.Channel.EnterTypingState())
            {
                await _database.RemoveTagSubscriptionAsync(Context.User.Id, tag);

                await ReplyAsync(_formatter.UnsubscribeSuccess(tag));
            }
        }

        [Command("clear")]
        public async Task ClearAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                // get tags user is subscribed to
                var tags = await _database.GetTagSubscriptionsAsync(Context.User.Id);

                // remove subscriptions in parallel
                await Task.WhenAll(tags.Select(t => _database.RemoveTagSubscriptionAsync(Context.User.Id, t)));

                await ReplyAsync(_formatter.SubscriptionsClearSuccess());
            }
        }
    }
}