using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using nhitomi.Database;
using nhitomi.Models.Queries;
using IElasticClient = nhitomi.Database.IElasticClient;

namespace nhitomi.Scrapers
{
    public interface IBookScraper : IScraper { }

    public abstract class BookScraperBase : ScraperBase, IBookScraper
    {
        readonly IElasticClient _client;

        protected BookScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<BookScraperBase> logger) : base(services, options, logger)
        {
            _client = services.GetService<IElasticClient>();
        }

        /// <summary>
        /// Scrapes new books without adding them to the database.
        /// </summary>
        protected abstract IAsyncEnumerable<DbBook> ScrapeAsync(CancellationToken cancellationToken = default);

        protected override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // it's better to fully enumerate scrape results before indexing them
            var books = await ScrapeAsync(cancellationToken).ToArrayAsync(cancellationToken);

            // index books one-by-one for effective merging
            foreach (var book in books)
                await IndexAsync(book, cancellationToken);
        }

        sealed class SimilarBookQuery : IQueryProcessor<DbBook>
        {
            readonly DbBook _book;

            public SimilarBookQuery(DbBook book)
            {
                _book = book;
            }

            public SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.SetMode(QueryMatchMode.All)
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Text(_book.PrimaryName, b => b.PrimaryName)
                                                               .Text(_book.EnglishName, b => b.EnglishName))
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Filter(new FilterQuery<string> { Values = _book.TagsArtist, Mode = QueryMatchMode.Any }, b => b.TagsArtist)
                                                               .Filter(new FilterQuery<string> { Values = _book.TagsCircle, Mode = QueryMatchMode.Any }, b => b.TagsCircle))
                                               .Filter(new FilterQuery<string> { Values = _book.TagsCharacter, Mode = QueryMatchMode.Any }, b => b.TagsCharacter));
        }

        protected async Task IndexAsync(DbBook book, CancellationToken cancellationToken = default)
        {
            // the database is structured so that "books" are containers of "contents" which are containers of "pages"
            // we consider two books to be the same if they have:
            // - matching primary or english name
            // - matching artist or circle
            // - at least one matching character

            var result = await _client.SearchEntriesAsync(new SimilarBookQuery(book), cancellationToken);

            if (result.Items.Length == 0)
            {
                // no similar books, so create a new one
                await _client.Entry(book).CreateAsync(cancellationToken);
            }
            else
            {
                // otherwise merge with similar
                var entry = result.Items[0];

                do
                {
                    if (entry.Value == null)
                    {
                        await IndexAsync(book, cancellationToken);
                        return;
                    }

                    entry.Value.MergeFrom(book);
                }
                while (!await entry.TryUpdateAsync(cancellationToken));
            }
        }
    }
}