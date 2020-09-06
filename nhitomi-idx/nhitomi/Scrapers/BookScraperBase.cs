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
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Storage;
using IElasticClient = nhitomi.Database.IElasticClient;

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

        public DbBook Convert(IScraper scraper, IServiceProvider services)
        {
            var book = new DbBook().ApplyBase(ModelSanitizer.Sanitize(Book), services);

            book.Contents = (Contents ?? Enumerable.Empty<ContentAdaptor>()).ToArray(c =>
            {
                var content = new DbBookContent().ApplyBase(ModelSanitizer.Sanitize(c.Content), services);

                content.PageCount   = c.Pages;
                content.Source      = scraper.Type;
                content.SourceId    = c.Id;
                content.Data        = c.Data;
                content.IsAvailable = true;
                content.RefreshTime = DateTime.UtcNow;

                return content;
            });

            return book;
        }
    }

    public interface IBookScraper : IScraper
    {
        string GetExternalUrl(DbBookContent content);

        /// <summary>
        /// Finds a book in the database given a book URL recognized by this scraper.
        /// Setting strict to false will allow multiple matches in the string; otherwise, the entire string will be attempted as one match.
        /// </summary>
        IAsyncEnumerable<(IDbEntry<DbBook> book, DbBookContent content)> FindByUrlAsync(string url, bool strict, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the image of a page of the given book content as a stream.
        /// </summary>
        Task<StorageFile> GetImageAsync(DbBookContent content, int index, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the latest book information for the given content.
        /// This can return null if the book was deleted from the source website.
        /// </summary>
        Task<DbBook> RetrieveAsync(DbBookContent content, CancellationToken cancellationToken = default);
    }

    public abstract class BookScraperBase<TState> : ScraperBase<TState>, IBookScraper
    {
        readonly IServiceProvider _services;
        readonly IElasticClient _client;
        readonly IBookIndexer _indexer;

        public override ScraperCategory Category => ScraperCategory.Book;

        protected BookScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<BookScraperBase<TState>> logger) : base(services, options, logger)
        {
            _services = services;
            _client   = services.GetService<IElasticClient>();
            _indexer  = services.GetService<IBookIndexer>();
        }

        public abstract string GetExternalUrl(DbBookContent content);

        /// <summary>
        /// Scrapes new books without adding them to the database.
        /// </summary>
        protected abstract IAsyncEnumerable<BookAdaptor> ScrapeAsync(TState state, CancellationToken cancellationToken = default);

        protected sealed override async Task RunAsync(TState state, CancellationToken cancellationToken = default)
            => await _indexer.IndexAsync(await ScrapeAsync(state, cancellationToken).Select(b => b.Convert(this, _services)).ToArrayAsync(cancellationToken), cancellationToken);

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

        public virtual async IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindByUrlAsync(string url, bool strict, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        public virtual Task<StorageFile> GetImageAsync(DbBookContent content, int index, CancellationToken cancellationToken = default)
            => Task.FromResult<StorageFile>(null);

        public virtual Task<BookAdaptor> RetrieveAsync(DbBookContent content, CancellationToken cancellationToken = default)
            => Task.FromResult<BookAdaptor>(null);

        async Task<DbBook> IBookScraper.RetrieveAsync(DbBookContent content, CancellationToken cancellationToken)
            => (await RetrieveAsync(content, cancellationToken))?.Convert(this, _services);
    }
}