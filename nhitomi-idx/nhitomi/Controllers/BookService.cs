using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    public class BookServiceOptions { }

    public interface IBookService
    {
        Task<OneOf<DbBook, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<OneOf<(DbBook, DbBookContent), NotFound>> GetContentAsync(string id, string contentId, CancellationToken cancellationToken = default);

        Task<SearchResult<DbBook>> SearchAsync(BookQuery query, CancellationToken cancellationToken = default);
        Task<BookSuggestResult> SuggestAsync(SuggestQuery query, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<OneOf<DbBook, NotFound>> UpdateAsync(string id, BookBase book, CancellationToken cancellationToken = default);
        Task<OneOf<(DbBook, DbBookContent), NotFound>> UpdateContentAsync(string id, string contentId, BookContentBase content, CancellationToken cancellationToken = default);

        Task<OneOf<Success, NotFound>> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<OneOf<DbBook, Success, NotFound>> DeleteContentAsync(string id, string contentId, CancellationToken cancellationToken = default);
    }

    public class BookService : IBookService
    {
        readonly IElasticClient _client;

        public BookService(IElasticClient client)
        {
            _client = client;
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

        public async Task<OneOf<DbBook, NotFound>> UpdateAsync(string id, BookBase book, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                if (!entry.Value.TryApplyBase(book))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<OneOf<(DbBook, DbBookContent), NotFound>> UpdateContentAsync(string id, string contentId, BookContentBase content, CancellationToken cancellationToken = default)
        {
            DbBookContent cont;

            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            do
            {
                cont = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

                if (cont == null)
                    return new NotFound();

                if (!cont.TryApplyBase(content))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return (entry.Value, cont);
        }

        public async Task<OneOf<Success, NotFound>> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();
            }
            while (!await entry.TryDeleteAsync(cancellationToken));

            return new Success();
        }

        public async Task<OneOf<DbBook, Success, NotFound>> DeleteContentAsync(string id, string contentId, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbBook>(id, cancellationToken);

            do
            {
                var content = entry.Value?.Contents.FirstOrDefault(c => c.Id == contentId);

                if (content == null)
                    return new NotFound();

                // if there is only one content, we should delete the entire book
                if (entry.Value.Contents.Length <= 1)
                {
                    if (await entry.TryDeleteAsync(cancellationToken))
                        return new Success();
                }

                // otherwise remove content
                else
                {
                    entry.Value.Contents = entry.Value.Contents.Where(c => c != content).ToArray();

                    if (await entry.TryUpdateAsync(cancellationToken))
                        return entry.Value;
                }
            }
            while (true);
        }
    }
}