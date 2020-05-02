using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using nhitomi.Database;
using nhitomi.Localization;
using nhitomi.Models;

namespace nhitomi.Discord.Commands
{
    public class BookMessage : InteractiveMessage
    {
        public (DbBook, DbBookContent) Book { get; set; }
        public bool Destroyable { get; set; } = true;

        readonly ILocale _l;
        readonly ILinkGenerator _link;

        public BookMessage(nhitomiCommandContext context, ILinkGenerator link)
        {
            _l    = context.Locale.Sections["get.book"];
            _link = link;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            if (Destroyable)
                yield return new DestroyTrigger(this);
        }

        protected override ReplyContent Render()
        {
            var (book, content) = Book;

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