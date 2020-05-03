using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using IElasticClient = nhitomi.Database.IElasticClient;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nhitomi.Scrapers
{
    /// <summary>
    /// Converts a book scraped from an arbitrary source to a model supported by nhitomi.
    /// </summary>
    public abstract class BookAdaptor
    {
        public abstract BookBase Book { get; }
        public abstract IEnumerable<ContentAdaptor> Contents { get; }

        public abstract class ContentAdaptor
        {
            public abstract string Id { get; }
            public abstract string Data { get; }
            public abstract int Pages { get; }
            public abstract BookContentBase Content { get; }
        }

        public DbBook Convert(IScraper scraper)
        {
            var book = new DbBook().ApplyBase(ModelSanitizer.Sanitize(Book));

            book.Contents = (Contents ?? Enumerable.Empty<ContentAdaptor>()).ToArray(c =>
            {
                var content = new DbBookContent().ApplyBase(ModelSanitizer.Sanitize(c.Content));

                content.Source   = scraper.Type;
                content.SourceId = c.Id;
                content.Data     = c.Data;
                content.Pages    = Enumerable.Range(0, c.Pages).ToArray(_ => new DbBookImage());

                return content;
            });

            return book;
        }
    }

    public interface IBookScraper : IScraper
    {
        /// <summary>
        /// Finds a book in the database given a book URL recognized by this scraper.
        /// Setting strict to false will allow multiple matches in the string; otherwise, the entire string will be attempted as one match.
        /// </summary>
        IAsyncEnumerable<(IDbEntry<DbBook> book, DbBookContent content)> FindBookByUrlAsync(string url, bool strict, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the image of a page of the given book content as a stream.
        /// </summary>
        Task<Stream> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default);
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
        protected abstract IAsyncEnumerable<BookAdaptor> ScrapeAsync(CancellationToken cancellationToken = default);

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
                                                               .Text($"\"{_book.PrimaryName}\"", b => b.PrimaryName) // quotes for phrase query
                                                               .Text($"\"{_book.EnglishName}\"", b => b.EnglishName))
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Text(new TextQuery { Values = _book.TagsArtist?.ToArray(s => $"\"{s}\""), Mode = QueryMatchMode.Any }, b => b.TagsArtist)
                                                               .Text(new TextQuery { Values = _book.TagsCircle?.ToArray(s => $"\"{s}\""), Mode = QueryMatchMode.Any }, b => b.TagsCircle)))
                              //.Filter(new FilterQuery<string> { Values = _book.TagsCharacter, Mode = QueryMatchMode.Any }, b => b.TagsCharacter))
                             .MultiSort(() => (SortDirection.Descending, null));
        }

        protected async Task IndexAsync(BookAdaptor adaptor, CancellationToken cancellationToken = default)
        {
            var book = adaptor.Convert(this);

            // the database is structured so that "books" are containers of "contents" which are containers of "pages"
            // we consider two books to be the same if they have:
            // - matching primary or english name
            // - matching artist or circle
            // // - at least one matching character (todo: temporarily disabled 2020/04/29)
            IDbEntry<DbBook> entry;

            if (book.TagsArtist?.Length > 0 || book.TagsCircle?.Length > 0) // && book.TagsCharacter?.Length > 0)
            {
                var result = await _client.SearchEntriesAsync(new SimilarQuery(book), cancellationToken);

                if (result.Items.Length != 0)
                {
                    // merge with similar
                    entry = result.Items[0];

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogInformation($"Merging {Type} book '{book.PrimaryName}' into similar book {entry.Id} '{entry.Value.PrimaryName}'.");

                    do
                    {
                        if (entry.Value == null)
                            goto create;

                        entry.Value.MergeFrom(book);
                    }
                    while (!await entry.TryUpdateAsync(cancellationToken));

                    return;
                }
            }

            create:

            // no similar books, so create a new one
            entry = _client.Entry(book);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogInformation($"Creating unique {Type} book {book.Id} '{book.PrimaryName}'.");

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
                var content = entry.Value.Contents?.FirstOrDefault(c => c.Source == Type && c.SourceId == id);

                if (content == null)
                    continue;

                yield return (entry, content);
            }
        }

        public abstract Task<Stream> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default);
    }
}