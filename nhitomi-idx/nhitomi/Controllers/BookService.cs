using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    public class BookServiceOptions
    {
        /// <summary>
        /// Enables an initial snapshot before the first modification to a book.
        /// This allows all history of a book to be preserved, because a snapshot is not created when a book is first indexed.
        /// </summary>
        public bool SnapshotBeforeInitialModification { get; set; } = true;
    }

    public interface IBookService
    {
        Task<OneOf<DbBook, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<OneOf<(DbBook, DbBookContent), NotFound>> GetContentAsync(string id, string contentId, CancellationToken cancellationToken = default);

        Task<SearchResult<DbBook>> SearchAsync(BookQuery query, CancellationToken cancellationToken = default);
        Task<BookSuggestResult> SuggestAsync(SuggestQuery query, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<OneOf<DbBook, NotFound>> UpdateAsync(string id, BookBase book, SnapshotArgs snapshot, CancellationToken cancellationToken = default);
        Task<OneOf<(DbBook, DbBookContent), NotFound>> UpdateContentAsync(string id, string contentId, BookContentBase content, SnapshotArgs snapshot, CancellationToken cancellationToken = default);

        Task<OneOf<Success, NotFound>> DeleteAsync(string id, SnapshotArgs snapshot, CancellationToken cancellationToken = default);
        Task<OneOf<DbBook, Success, NotFound>> DeleteContentAsync(string id, string contentId, SnapshotArgs snapshot, CancellationToken cancellationToken = default);
    }

    public class BookService : IBookService
    {
        readonly IElasticClient _client;
        readonly ISnapshotService _snapshots;
        readonly IOptionsMonitor<BookServiceOptions> _options;

        public BookService(IElasticClient client, ISnapshotService snapshots, IOptionsMonitor<BookServiceOptions> options)
        {
            _client    = client;
            _snapshots = snapshots;
            _options   = options;
        }

        public async Task<OneOf<DbBook, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var book = await _client.GetAsync<DbBook>(id, cancellationToken);

            if (book == null)
                return new NotFound();

            return book;
        }

        public async Task<OneOf<(DbBook, DbBookContent), NotFound>> GetContentAsync(string id, string contentId, CancellationToken cancellationToken = default)
        {
            var result = await GetAsync(id, cancellationToken);

            if (!result.TryPickT0(out var book, out var error))
                return error;

            var content = book.Contents.FirstOrDefault(c => c.Id == contentId);

            if (content == null)
                return new NotFound();

            return (book, content);
        }

        public Task<SearchResult<DbBook>> SearchAsync(BookQuery query, CancellationToken cancellationToken = default)
            => _client.SearchAsync(new DbBookQueryProcessor(query), cancellationToken);

        public Task<BookSuggestResult> SuggestAsync(SuggestQuery query, CancellationToken cancellationToken = default)
            => _client.SuggestAsync(new DbBookSuggestProcessor(query), cancellationToken);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => _client.CountAsync<DbBook>(cancellationToken);

        public async Task<OneOf<DbBook, NotFound>> UpdateAsync(string id, BookBase book, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            if (snapshot != null && entry.Value != null)
                await EnsureSnapshotBeforeModifyAsync(entry, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                if (!entry.Value.TryApplyBase(book))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            if (snapshot != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            return entry.Value;
        }

        public async Task<OneOf<(DbBook, DbBookContent), NotFound>> UpdateContentAsync(string id, string contentId, BookContentBase content, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            var cont = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

            if (snapshot != null && cont != null)
                await EnsureSnapshotBeforeModifyAsync(entry, cancellationToken);

            do
            {
                cont = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

                if (cont == null)
                    return new NotFound();

                if (!cont.TryApplyBase(content))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            if (snapshot != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            return (entry.Value, cont);
        }

        async Task EnsureSnapshotBeforeModifyAsync(IDbEntry<DbBook> entry, CancellationToken cancellationToken = default)
        {
            if (!_options.CurrentValue.SnapshotBeforeInitialModification)
                return;

            var result = await _snapshots.SearchAsync(ObjectType.Book, new SnapshotQuery { TargetId = entry.Id }, cancellationToken);

            if (result.Total != 0)
                return;

            await _snapshots.CreateAsync(entry.Value, new SnapshotArgs
            {
                Event  = SnapshotEvent.BeforeModification,
                Reason = $"Automatic snapshot before initial modification of book {entry.Id}.",
                Source = SnapshotSource.System,
                Time   = DateTime.UtcNow
            }, cancellationToken);
        }

        public async Task<OneOf<Success, NotFound>> DeleteAsync(string id, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            if (snapshot != null && entry.Value != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();
            }
            while (!await entry.TryDeleteAsync(cancellationToken));

            return new Success();
        }

        public async Task<OneOf<DbBook, Success, NotFound>> DeleteContentAsync(string id, string contentId, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            var cont = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

            if (snapshot != null && cont != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            do
            {
                cont = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

                if (cont == null)
                    return new NotFound();

                // if there is only one content, we should delete the entire book
                if (entry.Value.Contents.Length <= 1)
                {
                    if (await entry.TryDeleteAsync(cancellationToken))
                        return new Success();
                }

                // otherwise remove content and leave others remaining
                else
                {
                    entry.Value.Contents = entry.Value.Contents.Where(c => c != cont).ToArray();

                    if (await entry.TryUpdateAsync(cancellationToken))
                        return entry.Value;
                }
            }
            while (true);
        }
    }
}