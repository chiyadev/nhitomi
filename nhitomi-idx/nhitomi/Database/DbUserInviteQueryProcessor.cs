using System;
using System.Linq.Expressions;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    public class DbUserInviteQueryProcessor : QueryProcessorBase<DbUserInvite, UserInviteQuery>
    {
        public DbUserInviteQueryProcessor(UserInviteQuery query) : base(query) { }

        public override SearchDescriptor<DbUserInvite> Process(SearchDescriptor<DbUserInvite> descriptor)
            => base.Process(descriptor)
                   .MultiQuery(q => q.Filter(Query.Accepted, i => i.Accepted)
                                     .Filter(Query.InviterId, i => i.InviterId)
                                     .Filter(Query.InviteeId, i => i.InviteeId)
                                     .Range(Query.CreatedTime, i => i.CreatedTime)
                                     .Range(Query.ExpiryTime, i => i.ExpiryTime)
                                     .Range(Query.AcceptedTime, i => i.AcceptedTime))
                   .MultiSort(Query.Sorting, sort => sort switch
                    {
                        UserInviteSort.CreatedTime  => i => i.CreatedTime,
                        UserInviteSort.ExpiryTime   => i => i.ExpiryTime,
                        UserInviteSort.AcceptedTime => i => i.AcceptedTime,

                        _ => (Expression<Func<DbUserInvite, object>>) null
                    });
    }
}