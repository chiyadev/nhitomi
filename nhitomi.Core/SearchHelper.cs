using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class SearchHelper
    {
        public static IQueryable<T> FullTextSearch<T, TProperty>(this IQueryable<T> queryable, IDatabase db,
            Expression<Func<T, TProperty>> path, string query)
        {
            if (!(db is DbContext context))
                throw new ArgumentException($"{nameof(db)} is not a {nameof(DbContext)}.");

            switch (context.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql": return queryable.MySqlMatch(path, query);
                case "Microsoft.EntityFrameworkCore.Sqlite": return queryable.SqliteMatch(path, query);
            }

            throw new NotSupportedException($"Unknown database provider {context.Database.ProviderName}.");
        }

        //todo:
        public static IQueryable<T> MySqlMatch<T, TProperty>(this IQueryable<T> queryable,
            Expression<Func<T, TProperty>> path, string query) =>
            queryable;

        public static IQueryable<T> SqliteMatch<T, TProperty>(this IQueryable<T> queryable,
            Expression<Func<T, TProperty>> path, string query) =>
            queryable;
    }
}