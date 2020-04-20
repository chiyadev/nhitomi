using System;
using System.Linq.Expressions;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    public class DbUserQueryProcessor : QueryProcessorBase<DbUser, UserQuery>
    {
        public DbUserQueryProcessor(UserQuery query) : base(query) { }

        public override SearchDescriptor<DbUser> Process(SearchDescriptor<DbUser> descriptor)
            => base.Process(descriptor)
                   .MultiQuery(q => q.Range(Query.CreatedTime, u => u.CreatedTime)
                                     .Range(Query.UpdatedTime, u => u.UpdatedTime)
                                     .Filter(Query.Permissions, u => u.Permissions))
                   .MultiSort(Query.Sorting, sort => sort switch
                    {
                        UserSort.CreatedTime => u => u.CreatedTime,
                        UserSort.UpdatedTime => u => u.UpdatedTime,

                        _ => (Expression<Func<DbUser, object>>) null
                    });
    }
}