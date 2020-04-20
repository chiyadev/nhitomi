using System;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class SnapshotQuery : QueryBase<SnapshotSort>
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public FilterQuery<SnapshotType> Type { get; set; }
        public FilterQuery<SnapshotSource> Source { get; set; }
        public FilterQuery<string> RollbackId { get; set; }
        public FilterQuery<string> CommitterId { get; set; }
        public FilterQuery<string> TargetId { get; set; }
        public TextQuery Reason { get; set; }
    }
}