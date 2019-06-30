using System.Collections.Generic;
using System.Linq;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
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

        public class View : SynchronousListViewBase
        {
            new DoujinReadMessage Message => (DoujinReadMessage) base.Message;

            protected override int[] GetValues(int offset) =>
                Enumerable.Range(0, Message._doujin.PageCount).Skip(offset).ToArray();

            protected override Embed CreateEmbed(int value)
            {
                var doujin = Message._doujin;
                var l      = Context.GetLocalization()["doujinReadMessage"];

                return new EmbedBuilder
                {
                    Title       = doujin.OriginalName,
                    Description = l["text", new { page = value + 1, doujin }],
                    Url         = DoujinMessage.GetGalleryUrl(doujin),
                    ImageUrl    = $"https://nhitomi.chiya.dev/api/v1/images/{doujin.AccessId}/{value}",
                    Color       = Color.DarkGreen,

                    Author = new EmbedAuthorBuilder
                    {
                        Name    = doujin.GetTag(TagType.Artist)?.Value ?? doujin.Source,
                        IconUrl = DoujinMessage.GetSourceIconUrl(doujin)
                    },

                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{doujin.Source}/{doujin.SourceId}"
                    }
                }.Build();
            }

            protected override Embed CreateEmptyEmbed() => new EmbedBuilder
            {
                Title       = "Doujin has no pages",
                Description = "You should not be seeing this message. If you do, please report this as a bug."
            }.Build();

            protected override string ListBeginningMessage => "doujinReadMessage.listBeginning";
            protected override string ListEndMessage => "doujinReadMessage.listEnd";
        }
    }
}