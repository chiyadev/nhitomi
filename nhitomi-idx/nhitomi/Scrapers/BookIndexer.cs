using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using nhitomi.Database;
using nhitomi.Models.Queries;
using IElasticClient = nhitomi.Database.IElasticClient;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nhitomi.Scrapers
{
    public interface IBookIndexer
    {
        Task IndexAsync(DbBook[] books, CancellationToken cancellationToken = default);
    }

    public class BookIndexer : IBookIndexer
    {
        readonly IElasticClient _client;
        readonly IResourceLocker _locker;
        readonly ILogger<BookIndexer> _logger;

        public BookIndexer(IElasticClient client, IResourceLocker locker, ILogger<BookIndexer> logger)
        {
            _client = client;
            _locker = locker;
            _logger = logger;
        }

        public async Task IndexAsync(DbBook[] books, CancellationToken cancellationToken = default)
        {
            books = DryMerge(books);

            // prevent concurrent book indexing to allow efficient merging
            await using (await _locker.EnterAsync("index:book", cancellationToken))
            {
                // refresh immediately to speed up indexing
                using (_client.UseIndexingOptions(new IndexingOptions { Refresh = Refresh.True }))
                    await CreateAsync(await MergeAsync(books, cancellationToken), cancellationToken);
            }
        }

        static string FormatBook(DbBook book)
        {
            var s = $"'{book.PrimaryName}' {string.Join(',', book.Contents.Select(c => $"{c.Source}/{c.SourceId}"))}";

            if (book.Id != null)
                s = $"{book.Id} {s}";

            return s;
        }

        /// <summary>
        /// Efficiently merges books in-memory without making search requests.
        /// By deriving a unique list of books, we can reduce the number of database operations and, when possible, bulk create books that are not mergeable.
        /// </summary>
        DbBook[] DryMerge(DbBook[] books)
        {
            var measure = new MeasureContext();

            var results      = new List<DbBook>(books.Length);
            var primaryNames = new Dictionary<string, HashSet<DbBook>>(books.Length, StringComparer.OrdinalIgnoreCase);
            var englishNames = new Dictionary<string, HashSet<DbBook>>(books.Length, StringComparer.OrdinalIgnoreCase);
            var artistTags   = new Dictionary<string, HashSet<DbBook>>(StringComparer.OrdinalIgnoreCase);
            var circleTags   = new Dictionary<string, HashSet<DbBook>>(StringComparer.OrdinalIgnoreCase);

            static void union(HashSet<DbBook> set, Dictionary<string, HashSet<DbBook>> dict, string key)
            {
                if (string.IsNullOrEmpty(key)) return;

                if (dict.TryGetValue(key, out var other))
                    set.UnionWith(other);
            }

            static void index(DbBook book, Dictionary<string, HashSet<DbBook>> dict, string key)
            {
                if (string.IsNullOrEmpty(key)) return;

                if (dict.TryGetValue(key, out var set))
                    set.Add(book);
                else
                    dict[key] = new HashSet<DbBook> { book };
            }

            foreach (var book in books)
            {
                var set = new HashSet<DbBook>();

                union(set, primaryNames, book.PrimaryName);
                union(set, englishNames, book.EnglishName);

                var acSet = new HashSet<DbBook>();

                foreach (var artist in book.TagsArtist ?? Array.Empty<string>()) union(acSet, artistTags, artist);
                foreach (var circle in book.TagsCircle ?? Array.Empty<string>()) union(acSet, circleTags, circle);

                set.IntersectWith(acSet);

                var merged = false;

                foreach (var other in set)
                {
                    other.MergeFrom(book);

                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation($"Merged book {FormatBook(book)} into similar book {FormatBook(other)} (dry).");

                    merged = true;
                    break;
                }

                // only add unique (not merged) books to results and indexes
                if (!merged)
                {
                    results.Add(book);

                    index(book, primaryNames, book.PrimaryName);
                    index(book, englishNames, book.EnglishName);

                    foreach (var artist in book.TagsArtist ?? Array.Empty<string>()) index(book, artistTags, artist);
                    foreach (var circle in book.TagsCircle ?? Array.Empty<string>()) index(book, circleTags, circle);
                }
            }

            _logger.LogDebug($"Dry merging {books.Length} -> {results.Count} books took {measure}.");

            return results.ToArray();
        }

        // we consider two books to be the same if they have:
        // - matching primary or english name
        // - matching artist or circle
        // - at least one matching character (todo: temporarily disabled 2020/04/29)
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

        /// <summary>
        /// Merges books with existing ones in the database, and returns remaining books that were not merged.
        /// </summary>
        async Task<DbBook[]> MergeAsync(DbBook[] books, CancellationToken cancellationToken = default)
        {
            var list = new List<DbBook>(books.Length);

            foreach (var book in books)
            {
                if ((book.PrimaryName != null || book.EnglishName != null) &&
                    (book.TagsArtist?.Length > 0 || book.TagsCircle?.Length > 0))
                {
                    var result = await _client.SearchEntriesAsync(new SimilarQuery(book), cancellationToken);
                    var entry  = result.Items?.FirstOrDefault();

                    do
                    {
                        if (entry?.Value == null)
                            goto add;

                        entry.Value.MergeFrom(book);
                    }
                    while (!await entry.TryUpdateAsync(cancellationToken));

                    if (_logger.IsEnabled(LogLevel.Information))
                        _logger.LogInformation($"Merged book {FormatBook(book)} into similar book {FormatBook(entry.Value)}.");

                    continue;
                }

                add:
                list.Add(book);

                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation($"Merge skipping for book {FormatBook(book)}.");
            }

            return list.ToArray();
        }

        Task CreateAsync(DbBook[] books, CancellationToken cancellationToken = default) => _client.IndexManyAsync(books, OpType.Index, cancellationToken);
    }
}