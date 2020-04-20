using System;
using System.Linq.Expressions;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    public class DbBookQueryProcessor : QueryProcessorBase<DbBook, BookQuery>
    {
        public DbBookQueryProcessor(BookQuery query) : base(query) { }

        public override SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
            => base.Process(descriptor)
                   .MultiQuery(q =>
                    {
                        q = q.Range(Query.CreatedTime, b => b.CreatedTime)
                             .Range(Query.UpdatedTime, b => b.UpdatedTime)
                             .Text(Query.PrimaryName, b => b.PrimaryName)
                             .Text(Query.EnglishName, b => b.EnglishName);

                        if (Query.Tags != null)
                            foreach (var (k, v) in Query.Tags)
                            {
                                q = q.Text(v, k switch
                                {
                                    BookTag.Tag        => b => b.TagsGeneral,
                                    BookTag.Artist     => b => b.TagsArtist,
                                    BookTag.Parody     => b => b.TagsParody,
                                    BookTag.Character  => b => b.TagsCharacter,
                                    BookTag.Convention => b => b.TagsConvention,
                                    BookTag.Series     => b => b.TagsSeries,
                                    BookTag.Circle     => b => b.TagsCircle,
                                    BookTag.Metadata   => b => b.TagsMetadata,

                                    _ => (Expression<Func<DbBook, object>>) null
                                });
                            }

                        q = q.Filter(Query.Category, b => b.Category)
                             .Filter(Query.Rating, b => b.Rating)
                             .Range(Query.PageCount, b => b.PageCount)
                             .Filter(Query.Language, b => b.Language)
                             .Filter(Query.Sources?.Project(s => s.ToString()), b => b.Sources)
                             .Range(Query.Size, b => b.Size)
                             .Range(Query.Availability, b => b.Availability)
                             .Range(Query.TotalAvailability, b => b.TotalAvailability);

                        return q;
                    })
                   .MultiSort(Query.Sorting, sort => sort switch
                    {
                        BookSort.CreatedTime       => b => b.CreatedTime,
                        BookSort.UpdatedTime       => b => b.UpdatedTime,
                        BookSort.PageCount         => b => b.PageCount,
                        BookSort.Size              => b => b.Size,
                        BookSort.Availability      => b => b.Availability,
                        BookSort.TotalAvailability => b => b.TotalAvailability,

                        _ => (Expression<Func<DbBook, object>>) null
                    });
    }
}