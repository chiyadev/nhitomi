using System;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class UserInviteQuery : QueryBase<UserInviteSort>
    {
        public FilterQuery<bool> Accepted { get; set; }
        public FilterQuery<string> InviterId { get; set; }
        public FilterQuery<string> InviteeId { get; set; }
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> ExpiryTime { get; set; }
        public RangeQuery<DateTime> AcceptedTime { get; set; }
    }
}