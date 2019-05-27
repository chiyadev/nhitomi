using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class SearchHelper
    {
        public static IQueryable<T> FullTextSearch<T, TProperty>(this IQueryable<T> queryable, IDatabase db,
            string query, params Expression<Func<T, TProperty>>[] paths)
        {
            if (!(db is DbContext context))
                throw new ArgumentException($"{nameof(db)} is not a {nameof(DbContext)}.");

            switch (context.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql": return queryable.MySqlMatch(query, paths);
                case "Microsoft.EntityFrameworkCore.Sqlite": return queryable.SqliteMatch(query, paths);
            }

            throw new NotSupportedException($"Unknown database provider {context.Database.ProviderName}.");
        }

        //todo:
        public static IQueryable<T> MySqlMatch<T, TProperty>(this IQueryable<T> queryable,
            string query, params Expression<Func<T, TProperty>>[] paths) =>
            queryable;

        public static IQueryable<T> SqliteMatch<T, TProperty>(this IQueryable<T> queryable,
            string query, params Expression<Func<T, TProperty>>[] paths) =>
            queryable;
    }
}