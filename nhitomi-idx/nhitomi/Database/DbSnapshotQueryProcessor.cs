using System;
using System.Linq.Expressions;
using Nest;
using nhitomi.Models;
using nhitomi.Models.Queries;

namespace nhitomi.Database
{
    public class DbSnapshotQueryProcessor : QueryProcessorBase<DbSnapshot, SnapshotQuery>
    {
        // target moved out because it doesn't exist in query object
        readonly ObjectType _target;

        public DbSnapshotQueryProcessor(ObjectType target, SnapshotQuery query) : base(query)
        {
            _target = target;
        }

        public override SearchDescriptor<DbSnapshot> Process(SearchDescriptor<DbSnapshot> descriptor)
            => base.Process(descriptor)
                   .MultiQuery(q => q.Filter((FilterQuery<ObjectType>) _target, s => s.Target)
                                     .Range(Query.CreatedTime, s => s.CreatedTime)
                                     .Filter(Query.Source, s => s.Source)
                                     .Filter(Query.Event, s => s.Event)
                                     .Filter(Query.RollbackId, s => s.RollbackId)
                                     .Filter(Query.CommitterId, s => s.CommitterId)
                                     .Filter(Query.TargetId, s => s.TargetId)
                                     .Text(Query.Reason, s => s.Reason))
                   .MultiSort(Query.Sorting, sort => sort switch
                    {
                        SnapshotSort.CreatedTime => (DbSnapshot s) => s.CreatedTime,

                        _ => (Expression<Func<DbSnapshot, object>>) null
                    });
    }
}