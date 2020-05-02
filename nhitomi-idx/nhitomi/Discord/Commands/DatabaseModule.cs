using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nhitomi.Database;
using nhitomi.Localization;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Scrapers;
using Qmmands;

namespace nhitomi.Discord.Commands
{
    public class DatabaseModule : ModuleBase<nhitomiCommandContext>
    {
        readonly IElasticClient _client;
        readonly IScraperService _scrapers;

        public DatabaseModule(IElasticClient client, IScraperService scrapers)
        {
            _client   = client;
            _scrapers = scrapers;
        }

        [Command("get", "g"), Name("get")]
        public async Task GetAsync([Remainder] string link)
        {
            // try book scrapers
            var books = await _scrapers.Books.ToAsyncEnumerable().SelectMany(s => s.FindBookByUrlAsync(link, true)).Select(x => (x.Item1.Value, x.Item2)).ToArrayAsync();

            switch (books.Length)
            {
                case 0: break;

                case 1:
                    await Context.SendAsync<BookMessage>(m => m.Book = books[0]);
                    return;

                default:
                    await Context.SendAsync<BookListMessage>(m => m.Enumerator = books.ToAsyncEnumerable().GetNavigableAsyncEnumerator());
                    return;
            }

            await Context.SendAsync<GetNotFoundMessage>(m => m.Input = link);
        }

        [Command("view", "v", "read", "r"), Name("view")] // read is legacy command scheme
        public async Task ViewAsync([Remainder] string link)
        {
            // try book scrapers
            var (book, content) = await _scrapers.Books.ToAsyncEnumerable().SelectMany(s => s.FindBookByUrlAsync(link, true)).FirstOrDefaultAsync();

            if (book != null && content != null)
            {
                await Context.SendAsync<BookReadMessage>(m => m.Book = (book.Value, content));
                return;
            }

            await Context.SendAsync<GetNotFoundMessage>(m => m.Input = link);
        }

        [Command("from", "f"), Name("from")]
        public async Task FromAsync([Remainder] string source)
        {
            if (Enum.TryParse<ScraperType>(source, true, out var type) && _scrapers.Get(type, out var scraper))
                switch (scraper)
                {
                    case IBookScraper _:
                    {
                        var result = _client.SearchStreamAsync(new DbBookQueryProcessor(new BookQuery
                        {
                            Source = type,
                            Sorting =
                            {
                                (BookSort.CreatedTime, SortDirection.Descending)
                            }
                        }));

                        await Context.SendAsync<BookListMessage>(m => m.Enumerator = result.Select(b => (b, null as DbBookContent)).GetNavigableAsyncEnumerator());
                        return;
                    }
                }

            await Context.SendAsync<BadSourceMessage>(m => m.Input = source);
        }

        [Command("search", "s"), Name("search")]
        public Task SearchAsync([Remainder] string query)
        {
            var result = _client.SearchStreamAsync(new DbBookQueryProcessor(new BookQuery
            {
                Mode        = QueryMatchMode.Any,
                PrimaryName = query,
                EnglishName = query,
                Tags = new Dictionary<BookTag, TextQuery>().Chain(d =>
                {
                    foreach (BookTag tag in Enum.GetValues(typeof(BookTag)))
                        d[tag] = query;
                }),
                Sorting =
                {
                    (BookSort.Relevance, SortDirection.Descending)
                }
            }));

            return Context.SendAsync<BookListMessage>(m => m.Enumerator = result.Select(b => (b, null as DbBookContent)).GetNavigableAsyncEnumerator());
        }
    }

    public class GetNotFoundMessage : ReplyMessage
    {
        public string Input { get; set; }

        readonly ILocale _l;

        public GetNotFoundMessage(nhitomiCommandContext context)
        {
            _l = context.Locale.Sections["get.notFound"];
        }

        protected override ReplyContent Render() => new ReplyContent
        {
            Message = $@"
{_l["message", new { input = Input }]}

> - {_l["usageLink", new { ex = "https://nhentai.net/g/123/" }]}
> - {_l["usageSource", new { ex = "hitomi 123" }]}
".Trim()
        };
    }

    public class BadSourceMessage : ReplyMessage
    {
        public string Input { get; set; }

        readonly ILocale _l;
        readonly IScraperService _scrapers;

        public BadSourceMessage(nhitomiCommandContext context, IScraperService scrapers)
        {
            _l        = context.Locale.Sections["from.badSource"];
            _scrapers = scrapers;
        }

        protected override ReplyContent Render() => new ReplyContent
        {
            Message = $@"
{_l["message", new { input = Input }]}

{string.Join('\n', _scrapers.Select(s => $"> - {s.Type} — <{s.Url}>"))}
".Trim()
        };
    }
}