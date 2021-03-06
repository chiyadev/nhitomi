using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Collection))]
    public class DbCollection : DbObjectBase<Collection>, IDbHasType, IDbModelConvertible<DbCollection, Collection, CollectionBase>, IHasUpdatedTime
    {
        [IgnoreMember, Ignore]
        ObjectType IDbHasType.Type => ObjectType.Collection;

        [Key("na"), Text(Name = "na")]
        public string Name { get; set; }

        [Key("nd"), Text(Name = "nd")]
        public string Description { get; set; }

        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("ow"), Keyword(Name = "ow", DocValues = false)]
        public string[] OwnerIds { get; set; }

        [Key("ty"), Keyword(Name = "x", DocValues = false)]
        public ObjectType Type { get; set; }

        [Key("it"), Keyword(Name = "it", Index = false)]
        public string[] Items { get; set; }

        public override void MapTo(Collection model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Name        = Name;
            model.Description = Description;
            model.CreatedTime = CreatedTime;
            model.UpdatedTime = UpdatedTime;
            model.OwnerIds    = OwnerIds;
            model.Type        = Type;
            model.Items       = Items;
        }

        public override void MapFrom(Collection model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Name        = model.Name;
            Description = model.Description;
            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            OwnerIds    = model.OwnerIds;
            Type        = model.Type;
            Items       = model.Items;
        }
    }
}