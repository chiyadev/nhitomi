using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class SearchHelper
    {
        public static IQueryable<T> FullTextSearch<T>(this IQueryable<T> queryable, IDatabase db, string query)
            where T : class
        {
            if (!(db is DbContext context))
                throw new ArgumentException($"{nameof(db)} is not a {nameof(DbContext)}.");

            switch (context.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql": return queryable.MySqlMatch(query);
                case "Microsoft.EntityFrameworkCore.Sqlite": return queryable.SqliteMatch(query);
            }

            throw new NotSupportedException($"Unknown database provider {context.Database.ProviderName}.");
        }

        static IQueryable<T> MySqlMatch<T>(this IQueryable<T> queryable, string query)
            where T : class =>
            //todo: optimize this dumb shit
            queryable.FromSql(@"
SELECT *
FROM `Doujins`
WHERE
    MATCH `PrettyName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
    MATCH `OriginalName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
    `Doujins`.`Id` IN (
        SELECT `DoujinId`
        FROM `TagRef`
        WHERE `TagRef`.`TagId` IN (
            SELECT `Id`
            FROM `Tags`
            WHERE MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE)))
ORDER BY
    MATCH `PrettyName` AGAINST ({0} IN NATURAL LANGUAGE MODE) +
    MATCH `OriginalName` AGAINST ({0} IN NATURAL LANGUAGE MODE) DESC", query);

        //todo:
        static IQueryable<T> SqliteMatch<T>(this IQueryable<T> queryable, string query) =>
            queryable;
    }
}
