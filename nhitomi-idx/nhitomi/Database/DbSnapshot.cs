using System;
using MessagePack;
using Nest;
using nhitomi.Models;
using Snapshot = nhitomi.Models.Snapshot;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a snapshot of an object.
    /// The actual object can be serialized and stored in <see cref="Data"/> or persisted on an external storage depending on its size.
    /// Snapshot data may not always be available.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Snapshot))]
    public class DbSnapshot : DbObjectBase<Snapshot>, IDbHasType, IDbModelConvertible<DbSnapshot, Snapshot>, IHasCreatedTime
    {
        [IgnoreMember, Ignore]
        ObjectType IDbHasType.Type => ObjectType.Snapshot;

        [Key("T"), Date(Name = "T")]
        public DateTime CreatedTime { get; set; }

        [Key("s"), Keyword(Name = "s", DocValues = false)]
        public SnapshotSource Source { get; set; }

        [Key("e"), Keyword(Name = "e", DocValues = false)]
        public SnapshotEvent Event { get; set; }

        [Key("b"), Keyword(Name = "b", DocValues = false)]
        public string RollbackId { get; set; }

        [Key("c"), Keyword(Name = "c", DocValues = false)]
        public string CommitterId { get; set; }

        [Key("x"), Keyword(Name = "x", DocValues = false)]
        public ObjectType Target { get; set; }

        [Key("z"), Keyword(Name = "z", DocValues = false)]
        public string TargetId { get; set; }

        [Key("r"), Text(Name = "r")]
        public string Reason { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("d"), Keyword(Name = "d", Index = false)]
        public string Data { get; set; }

        public override void MapTo(Snapshot model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.CreatedTime = CreatedTime;
            model.Source      = Source;
            model.Event       = Event;
            model.RollbackId  = RollbackId;
            model.CommitterId = CommitterId;
            model.Target      = Target;
            model.TargetId    = TargetId;
            model.Reason      = Reason;
        }

        public override void MapFrom(Snapshot model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            CreatedTime = model.CreatedTime;
            Source      = model.Source;
            Event       = model.Event;
            RollbackId  = model.RollbackId;
            CommitterId = model.CommitterId;
            Target      = model.Target;
            TargetId    = model.TargetId;
            Reason      = model.Reason;
        }
    }
}