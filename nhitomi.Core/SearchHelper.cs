using System;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class DoujinSearchArguments
    {
        public string Query { get; set; }
        public bool QualityFilter { get; set; } = true;
    }

    public static class SearchHelper
    {
        public static IQueryable<T> FullTextSearch<T>(this IQueryable<T> queryable, IDatabase db,
            DoujinSearchArguments args)
            where T : class
        {
            if (!(db is DbContext context))
                throw new ArgumentException($"{nameof(db)} is not a {nameof(DbContext)}.");

            switch (context.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql": return queryable.MySqlMatch(args);
                case "Microsoft.EntityFrameworkCore.Sqlite": return queryable.SqliteMatch(args);
            }

            throw new NotSupportedException($"Unknown database provider {context.Database.ProviderName}.");
        }

        static IQueryable<T> MySqlMatch<T>(this IQueryable<T> queryable, DoujinSearchArguments args)
            where T : class
        {
            var sql = new StringBuilder().AppendLine(@"
SELECT *, (
  SELECT SUM(MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE))
  FROM `Tags`
  WHERE
    `Id` IN (
      SELECT `TagId`
      FROM `TagRef`
      WHERE `DoujinId` = `Doujins`.`Id`) AND
    MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE)
  ) AS TagRelevance
FROM `Doujins`
WHERE");

            if (args.QualityFilter)
            {
                // 327 is full color tag
                sql.AppendLine(@"
  `Id` IN (
    SELECT `DoujinId`
    FROM `TagRef`
    WHERE `TagId` = 327) AND (");
            }

            sql.AppendLine(@"
  MATCH `PrettyName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
  MATCH `OriginalName` AGAINST ({0} IN NATURAL LANGUAGE MODE) OR
  `Id` IN (
    SELECT `DoujinId`
    FROM `TagRef`
    WHERE `TagId` IN (
      SELECT `Id`
      FROM `Tags`
      WHERE MATCH `Value` AGAINST ({0} IN NATURAL LANGUAGE MODE)))");

            if (args.QualityFilter)
                sql.AppendLine(")");

            sql.AppendLine(@"
ORDER BY
  `TagRelevance` DESC,
  `UploadTime` DESC,
  `ProcessTime` DESC");

            return queryable.FromSql(sql.ToString(), args.Query);
        }

        //todo:
        static IQueryable<T> SqliteMatch<T>(this IQueryable<T> queryable, DoujinSearchArguments args) =>
            queryable;
    }
}