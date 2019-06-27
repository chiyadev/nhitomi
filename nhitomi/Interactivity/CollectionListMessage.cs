using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Globalization;
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

            protected override Embed CreateEmbed(Collection collection)
            {
                var path = new LocalizationPath("collectionMessage");
                var l    = Context.GetLocalization();

                var embed = new EmbedBuilder()
                           .WithTitle(path["title"][l, new { context = Context, collection }])
                           .WithColor(Color.Teal);

                if (collection.Doujins.Count == 0)
                {
                    embed.Description = path["emptyCollection"][l];
                }
                else
                {
                    var first = collection.Doujins.First().DoujinId;

                    embed.ThumbnailUrl = $"https://nhitomi.chiya.dev/api/v1/images/{first}/-1";
                }

                embed.AddField(path["sort"][l],     path["sortValues"][collection.Sort.ToString()][l]);
                embed.AddField(path["contents"][l], path["contentsValue"][l, new { collection }]);

                return embed.Build();
            }

            protected override Embed CreateEmptyEmbed()
            {
                var path = new LocalizationPath("collectionMessage.empty");
                var l    = Context.GetLocalization();

                return new EmbedBuilder()
                      .WithTitle(path["title"][l, new { context = Context }])
                      .WithColor(Color.Teal)
                      .WithDescription(path["text"][l, new { context = Context }])
                      .Build();
            }
        }
    }
}