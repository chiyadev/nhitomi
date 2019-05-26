using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class QueryableUtility
    {
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable)
            where T : class =>
            AsyncEnumerable.CreateEnumerable(() =>
            {
                var index = 0;
                var current = null as T;

                return AsyncEnumerable.CreateEnumerator(
                    async token => (current = await queryable
                                       .Skip(index++)
                                       .FirstOrDefaultAsync(token)) != null,
                    () => current,
                    () => { });
            });

        public static IAsyncEnumerable<T> ToChunkedAsyncEnumerable<T>(this IQueryable<T> queryable, int chunkSize)
            where T : class =>
            AsyncEnumerable.CreateEnumerable(() =>
            {
                var index = 0;
                var current = new T[0];

                return AsyncEnumerable.CreateEnumerator(
                    async token => (current = await queryable
                                       .Skip(chunkSize * index++)
                                       .Take(chunkSize)
                                       .ToArrayAsync(token)).Length != 0,
                    () => current,
                    () => { });
            }).SelectMany(x => x.ToAsyncEnumerable());
    }
}