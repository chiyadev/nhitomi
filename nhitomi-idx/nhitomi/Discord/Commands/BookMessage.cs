using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Database;
using nhitomi.Localization;
using nhitomi.Models;

namespace nhitomi.Discord.Commands
{
    public class BookMessage : InteractiveMessage
    {
        public DbBook Book { get; set; }
        public (DbBook book, DbBookContent content)? Current { get; private set; }
        public bool Destroyable { get; set; } = true;

        readonly nhitomiCommandContext _context;
        readonly ILocale _l;
        readonly ILinkGenerator _link;
        readonly IBookContentSelector _selector;

        public BookMessage(nhitomiCommandContext context, ILinkGenerator link, IBookContentSelector selector)
        {
            _context  = context;
            _l        = context.Locale.Sections["get.book"];
            _link     = link;
            _selector = selector;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new BookReadTrigger(_context, () => Current);

            if (Destroyable)
                yield return new DestroyTrigger(this);
        }

        protected override async Task<ReplyContent> RenderAsync(CancellationToken cancellationToken = default)
        {
            var (book, content) = Current ??= (Book, await _selector.SelectAsync(Book, _context, cancellationToken));

            return Render(book, content, _l, _link);
        }

        public static ReplyContent Render(DbBook book, DbBookContent content, ILocale l, ILinkGenerator link) => new ReplyContent
        {
            Embed = new EmbedBuilder
            {
                Title       = book.PrimaryName,
                Description = book.EnglishName == book.PrimaryName ? null : book.EnglishName,
                Url         = link.GetWebLink($"books/{book.Id}/contents/{content.Id}?auth=discord"),
                ImageUrl    = link.GetApiLink($"books/{book.Id}/contents/{content.Id}/pages/0/thumb"),
                Color       = Color.Green,

                Author = new EmbedAuthorBuilder
                {
                    Name    = book.TagsArtist?.Length > 0 ? string.Join(", ", book.TagsArtist) : content.Source.ToString(),
                    IconUrl = link.GetWebLink($"assets/icons/{content.Source}.jpg")
                },

                Footer = new EmbedFooterBuilder
                {
                    Text = $"{content.Source}/{content.SourceId} — {book.Id}/{content.Id}"
                },

                Fields = Enum.GetValues(typeof(BookTag))
                             .Cast<BookTag>()
                             .Where(t => book.GetTags(t)?.Length >= 0)
                             .ToList(t => new EmbedFieldBuilder
                              {
                                  Name     = l.Sections["tags"][t.ToString()],
                                  Value    = string.Join(", ", book.GetTags(t)),
                                  IsInline = true
                              })
            }
        };
    }
}