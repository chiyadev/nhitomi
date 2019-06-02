using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class CollectionListMessage : ListMessage<CollectionListMessage.View, Collection>
    {
        readonly ulong _userId;

        public CollectionListMessage(ulong userId)
        {
            _userId = userId;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        public class View : ListViewBase
        {
            new CollectionListMessage Message => (CollectionListMessage) base.Message;

            readonly IDatabase _db;

            public View(IDatabase db)
            {
                _db = db;
            }

            protected override Task<Collection[]> GetValuesAsync(int offset,
                CancellationToken cancellationToken = default) =>
                offset == 0
                    ? _db.GetCollectionsAsync(Message._userId, cancellationToken)
                    : Task.FromResult(new Collection[0]);

            protected override Embed CreateEmbed(Collection value)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"**{Context.User.Username}**: {value.Name}")
                    .WithColor(Color.Teal)
                    .WithCurrentTimestamp();

                if (value.Doujins.Count == 0)
                    embed.Description = "Empty collection";
                else
                    embed.ThumbnailUrl = $"https://nhitomi.chiya.dev/v1/image/{value.Doujins.First().DoujinId}/-1";

                var sort = value.Sort.ToString();

                if (value.SortDescending)
                    sort += " (desc)";

                embed.AddField("Sort", sort);
                embed.AddField("Contents", $"{value.Doujins.Count} doujins");

                return embed.Build();
            }

            protected override Embed CreateEmptyEmbed() => throw new System.NotImplementedException();
        }
    }
}