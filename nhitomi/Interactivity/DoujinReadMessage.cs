using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;
using TagType = nhitomi.Core.TagType;

namespace nhitomi.Interactivity
{
    public class DoujinReadMessage : ListMessage<DoujinReadMessage.View, int>
    {
        readonly Doujin _doujin;

        public DoujinReadMessage(Doujin doujin)
        {
            _doujin = doujin;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new ListJumpTrigger(JumpDestination.Start);

            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new ListJumpTrigger(JumpDestination.End, _doujin.PageCount - 1);
            yield return new DeleteTrigger();
        }

        public class View : ListViewBase
        {
            new DoujinReadMessage Message => (DoujinReadMessage) base.Message;

            protected override Task<int[]> GetValuesAsync(int offset,
                CancellationToken cancellationToken = default) =>
                offset == 0
                    ? Task.FromResult(Enumerable.Range(0, Message._doujin.PageCount).ToArray())
                    : Task.FromResult<int[]>(null);

            protected override Embed CreateEmbed(int value)
            {
                var doujin = Message._doujin;

                var path = new LocalizationPath("doujinReadMessage");
                var l = Context.GetLocalization();

                return new EmbedBuilder()
                    .WithTitle(path["title"][l, new {doujin}])
                    .WithDescription(path["text"][l, new {page = value + 1, doujin}])
                    .WithAuthor(a => a
                        .WithName(doujin.GetTag(TagType.Artist)?.Value ?? doujin.Source)
                        .WithIconUrl(DoujinMessage.View.GetSourceIconUrl(doujin)))
                    .WithUrl(DoujinMessage.View.GetGalleryUrl(doujin))
                    .WithImageUrl($"https://nhitomi.chiya.dev/api/v1/images/{doujin.AccessId}/{value}")
                    .WithColor(Color.DarkGreen)
                    .WithFooter(path["footer"][l, new {doujin}])
                    .Build();
            }

            protected override Embed CreateEmptyEmbed() => new EmbedBuilder()
                .WithTitle("Doujin has no pages.")
                .WithDescription("You should not be seeing this message. " +
                                 "If you do, please report this as a bug.")
                .Build();
        }
    }
}