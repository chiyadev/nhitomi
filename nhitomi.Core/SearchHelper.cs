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
SELECT *,
  MATCH `PrettyName` AGAINST ({0} IN NATURAL LANGUAGE MODE) AS PrettyNameRelevance,
  MATCH `OriginalName` AGAINST ({0} IN NATURAL LANGUAGE MODE) AS OriginalNameRelevance,
  (SELECT
     SUM(MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE))
     FROM `Tags`
     WHERE
       `Id` IN
         (SELECT `TagId`
          FROM `TagRef`
          WHERE `DoujinId` = `Doujins`.`Id`) AND
       MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE)) AS TagRelevance
FROM `Doujins`
WHERE
  MATCH `PrettyName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
  MATCH `OriginalName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
  `Id` IN
    (SELECT `DoujinId`
     FROM `TagRef`
     WHERE `TagId` IN
       (SELECT `Id`
        FROM `Tags`
        WHERE
          MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE)))
ORDER BY
  `PrettyNameRelevance` + `OriginalNameRelevance` + `TagRelevance` DESC,
  `UploadTime` DESC,
  `ProcessTime` DESC", query);

        //todo:
        static IQueryable<T> SqliteMatch<T>(this IQueryable<T> queryable, string query) =>
            queryable;
    }
}