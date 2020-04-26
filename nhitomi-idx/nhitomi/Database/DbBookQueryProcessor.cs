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
                        q = q.SetMode(Query.Mode)
                             .Range(Query.CreatedTime, b => b.CreatedTime)
                             .Range(Query.UpdatedTime, b => b.UpdatedTime)
                             .Range(Query.PageCount, b => b.PageCount)
                             .Range(Query.NoteCount, b => b.NoteCount)
                             .Range(Query.TagCount, b => b.TagCount)
                             .Text(Query.PrimaryName, b => b.PrimaryName)
                             .Text(Query.EnglishName, b => b.EnglishName);

                        if (Query.Tags != null)
                            foreach (var (k, v) in Query.Tags)
                            {
                                q = q.Text(v, (Expression<Func<DbBook, object>>) (k switch
                                {
                                    BookTag.Tag        => b => b.TagsGeneral,
                                    BookTag.Artist     => b => b.TagsArtist,
                                    BookTag.Parody     => b => b.TagsParody,
                                    BookTag.Character  => b => b.TagsCharacter,
                                    BookTag.Convention => b => b.TagsConvention,
                                    BookTag.Series     => b => b.TagsSeries,
                                    BookTag.Circle     => b => b.TagsCircle,
                                    BookTag.Metadata   => b => b.TagsMetadata,

                                    _ => null
                                }));
                            }

                        q = q.Filter(Query.Category, b => b.Category)
                             .Filter(Query.Rating, b => b.Rating)
                             .Filter(Query.Language, b => b.Language)
                             .Filter(Query.Source?.Project(s => s.ToString()), b => b.Sources);

                        return q;
                    })
                   .MultiSort(Query.Sorting, sort => (Expression<Func<DbBook, object>>) (sort switch
                    {
                        BookSort.CreatedTime => b => b.CreatedTime,
                        BookSort.UpdatedTime => b => b.UpdatedTime,
                        BookSort.PageCount   => b => b.PageCount,
                        BookSort.NoteCount   => b => b.NoteCount,
                        BookSort.TagCount    => b => b.TagCount,

                        _ => null
                    }));
    }
}