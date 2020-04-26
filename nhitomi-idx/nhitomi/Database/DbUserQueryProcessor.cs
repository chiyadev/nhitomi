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
                   .MultiQuery(q => q.SetMode(Query.Mode)
                                     .Range(Query.CreatedTime, u => u.CreatedTime)
                                     .Range(Query.UpdatedTime, u => u.UpdatedTime)
                                     .Filter(Query.Username, u => u.Username)
                                     .Filter(Query.Email, u => u.Email)
                                     .Filter(Query.Permissions, u => u.Permissions))
                   .MultiSort(Query.Sorting, sort => (Expression<Func<DbUser, object>>) (sort switch
                    {
                        UserSort.CreatedTime => u => u.CreatedTime,
                        UserSort.UpdatedTime => u => u.UpdatedTime,
                        UserSort.Username    => u => u.Username,
                        UserSort.Email       => u => u.Email,

                        _ => null
                    }));
    }
}