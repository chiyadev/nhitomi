using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using nhitomi.Database;
using nhitomi.Models.Queries;
using IElasticClient = nhitomi.Database.IElasticClient;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nhitomi.Scrapers
{
    public interface IBookScraper : IScraper
    {
        IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindBookByUrlAsync(string url, bool strict, CancellationToken cancellationToken = default);
    }

    public abstract class BookScraperBase : ScraperBase, IBookScraper
    {
        readonly IElasticClient _client;
        readonly ILogger<BookScraperBase> _logger;

        protected BookScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<BookScraperBase> logger) : base(services, options, logger)
        {
            _client = services.GetService<IElasticClient>();
            _logger = logger;
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

        sealed class SimilarQuery : IQueryProcessor<DbBook>
        {
            readonly DbBook _book;

            public SimilarQuery(DbBook book)
            {
                _book = book;
            }

            public SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.SetMode(QueryMatchMode.All)
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Text($"\"{_book.PrimaryName}\"", b => b.PrimaryName) // quote for phrase query
                                                               .Text($"\"{_book.EnglishName}\"", b => b.EnglishName))
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Filter(new FilterQuery<string> { Values = _book.TagsArtist, Mode = QueryMatchMode.Any }, b => b.TagsArtist)
                                                               .Filter(new FilterQuery<string> { Values = _book.TagsCircle, Mode = QueryMatchMode.Any }, b => b.TagsCircle))
                                               .Filter(new FilterQuery<string> { Values = _book.TagsCharacter, Mode = QueryMatchMode.Any }, b => b.TagsCharacter))
                             .MultiSort(() => (SortDirection.Descending, null));
        }

        protected virtual DbBook Sanitize(DbBook book) => book.Apply(ModelSanitizer.Sanitize(book.Convert()));

        protected async Task IndexAsync(DbBook book, CancellationToken cancellationToken = default)
        {
            book = Sanitize(book);

            // the database is structured so that "books" are containers of "contents" which are containers of "pages"
            // we consider two books to be the same if they have:
            // - matching primary or english name
            // - matching artist or circle
            // - at least one matching character
            IDbEntry<DbBook> entry;

            if ((book.TagsArtist?.Length > 0 || book.TagsCircle?.Length > 0) && book.TagsCharacter?.Length > 0)
            {
                var result = await _client.SearchEntriesAsync(new SimilarQuery(book), cancellationToken);

                if (result.Items.Length != 0)
                {
                    // merge with similar
                    entry = result.Items[0];

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"Merging {Type} book '{book.PrimaryName}' into similar book {entry.Id} '{entry.Value.PrimaryName}'.");

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

                    return;
                }
            }

            // no similar books, so create a new one
            entry = _client.Entry(book);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Creating unique {Type} book {book.Id} '{book.PrimaryName}'.");

            await entry.CreateAsync(cancellationToken);
        }

        sealed class SourceQuery : IQueryProcessor<DbBook>
        {
            readonly ScraperType _type;
            readonly string _id;

            public SourceQuery(ScraperType type, string id)
            {
                _type = type;
                _id   = id;
            }

            public SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.Filter((FilterQuery<ScraperType>) _type, b => b.Sources)
                                               .Filter((FilterQuery<string>) _id, b => b.SourceIds))
                             .MultiSort(() => (SortDirection.Descending, null));
        }

        public async IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindBookByUrlAsync(string url, bool strict, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (UrlRegex == null)
                yield break;

            foreach (var id in UrlRegex.MatchIds(url, strict))
            {
                var result = await _client.SearchEntriesAsync(new SourceQuery(Type, id), cancellationToken);

                if (result.Items.Length == 0)
                    continue;

                var entry   = result.Items[0];
                var content = entry.Value.Contents?.FirstOrDefault(c => c.SourceId == id);

                if (content == null)
                    continue;

                yield return (entry, content);
            }
        }
    }
}