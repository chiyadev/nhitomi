using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Database;
using nhitomi.Localization;

namespace nhitomi.Discord.Commands
{
    public class BookReadMessage : InteractiveMessage, IListJumpTriggerTarget
    {
        readonly ILocale _l;
        readonly ILinkGenerator _link;

        public (DbBook book, DbBookContent content) Book { get; set; }
        public int Position { get; private set; }
        public int End => Book.content.Pages.Length - 1;

        public BookReadMessage(nhitomiCommandContext context, ILinkGenerator link)
        {
            _l    = context.Locale.Sections["view.book"];
            _link = link;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new ListTrigger(this, ListTriggerDirection.Left);
            yield return new ListTrigger(this, ListTriggerDirection.Right);
            yield return new ListJumpTrigger(this, ListJumpTriggerDestination.Input);
            yield return new DestroyTrigger(this);
        }

        public Task<bool> SetPositionAsync(int position, CancellationToken cancellationToken = default)
        {
            if (0 <= position && position <= End)
            {
                Position = position;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        protected override ReplyContent Render()
        {
            var (book, content) = Book;

            return new ReplyContent
            {
                Embed = new EmbedBuilder
                {
                    Title       = book.PrimaryName,
                    Description = _l["pagination", new { current = Position + 1, total = End + 1 }],
                    Url         = _link.GetWebLink($"books/{book.Id}/contents/{content.Id}?auth=discord"),
                    ImageUrl    = _link.GetApiLink($"books/{book.Id}/contents/{content.Id}/pages/{Position}"),
                    Color       = Color.DarkGreen,

                    Author = new EmbedAuthorBuilder
                    {
                        Name    = book.TagsArtist?.Length > 0 ? string.Join(", ", book.TagsArtist) : content.Source.ToString(),
                        IconUrl = _link.GetWebLink($"assets/icons/{content.Source}.jpg")
                    },

                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{book.Id}/{content.Id}"
                    }
                }
            };
        }
    }
}