using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Database;
using nhitomi.Localization;
using nhitomi.Models;
using nhitomi.Scrapers;

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
        readonly IScraperService _scrapers;

        public BookMessage(nhitomiCommandContext context, ILinkGenerator link, IBookContentSelector selector, IScraperService scrapers)
        {
            _context  = context;
            _l        = context.Locale.Sections["get.book"];
            _link     = link;
            _selector = selector;
            _scrapers = scrapers;
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

            return Render(book, content, _l, _link, _scrapers);
        }

        public static ReplyContent Render(DbBook book, DbBookContent content, ILocale l, ILinkGenerator link, IScraperService scrapers) => new ReplyContent
        {
            Message = string.Join('\n', book.Contents.GroupBy(c => (c.Source, c.Language)).OrderBy(g => g.Key.Source).ThenBy(g => g.Key.Language).Select(group =>
            {
                var (source, language) = group.Key;

                var urls = string.Join(' ', group.Select(c =>
                {
                    if (!scrapers.GetBook(c.Source, out var scraper))
                        return null;

                    var url = scraper.GetExternalUrl(book, c);

                    if (c == content)
                        url = $"**{url}**";

                    return url;
                }).Where(x => x != null));

                return $"{source} ({language}): {urls}";
            })),
            Embed = new EmbedBuilder
            {
                Title       = book.PrimaryName,
                Description = book.EnglishName == book.PrimaryName ? null : book.EnglishName,
                Url         = link.GetWebLink($"books/{book.Id}/contents/{content.Id}?auth=discord"),
                ImageUrl    = link.GetApiLink($"books/{book.Id}/contents/{content.Id}/pages/0/thumb"),
                Color       = Color.Green,

                Author = new EmbedAuthorBuilder
                {
                    Name    = string.Join(", ", book.TagsArtist ?? book.TagsCircle ?? new[] { content.Source.ToString() }),
                    IconUrl = link.GetWebLink($"assets/icons/{content.Source}.jpg")
                },

                Footer = new EmbedFooterBuilder
                {
                    Text = $"{book.Id}/{content.Id}"
                },

                Fields = Enum.GetValues(typeof(BookTag))
                             .Cast<BookTag>()
                             .Where(t => book.GetTags(t)?.Length >= 0)
                             .ToList(t => new EmbedFieldBuilder
                              {
                                  Name     = l.Sections["tags"][t.ToString()],
                                  Value    = string.Join(", ", book.GetTags(t).OrderBy(x => x)),
                                  IsInline = true
                              })
            }
        };
    }
}