using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a snapshot of an object.
    /// The actual object can be serialized and stored in <see cref="Data"/> or persisted on an external storage depending on its size.
    /// Snapshot data may not always be available.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Models.Snapshot))]
    public class DbSnapshot : DbObjectBase<Models.Snapshot>, IDbModelConvertible<DbSnapshot, Models.Snapshot>, IHasCreatedTime
    {
        [Key("T"), Date(Name = "T")]
        public DateTime CreatedTime { get; set; }

        [Key("s"), Keyword(Name = "s")]
        public SnapshotSource Source { get; set; }

        [Key("e"), Keyword(Name = "e")]
        public SnapshotEvent Event { get; set; }

        [Key("b"), Keyword(Name = "b")]
        public string RollbackId { get; set; }

        [Key("c"), Keyword(Name = "c")]
        public string CommitterId { get; set; }

        [Key("x"), Keyword(Name = "x")]
        public ObjectType Target { get; set; }

        [Key("z"), Keyword(Name = "z")]
        public string TargetId { get; set; }

        [Key("r"), Text(Name = "r")]
        public string Reason { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("d"), Keyword(Name = "d", Index = false)]
        public string Data { get; set; }

        public override void MapTo(Models.Snapshot model)
        {
            base.MapTo(model);

            model.CreatedTime = CreatedTime;
            model.Source      = Source;
            model.Event       = Event;
            model.RollbackId  = RollbackId;
            model.CommitterId = CommitterId;
            model.Target      = Target;
            model.TargetId    = TargetId;
            model.Reason      = Reason;
        }

        public override void MapFrom(Models.Snapshot model)
        {
            base.MapFrom(model);

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