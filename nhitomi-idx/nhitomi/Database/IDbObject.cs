using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    public interface IDbObject : IHasId, IDbModel
    {
        /// <summary>
        /// Updates cached properties used for querying.
        /// </summary>
        void UpdateCache(IServiceProvider services);
    }

    public interface IDbObject<in T> : IDbObject, IDbModel<T> where T : IHasId { }

    public interface IDbHasType
    {
        ObjectType Type { get; }
    }

    public abstract class DbObjectBase<T> : DbModelBase<T>, IDbObject<T> where T : IHasId
    {
        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("id"), Keyword(Name = "id", Index = false)]
        public string Id { get; set; }

        public override void MapTo(T model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Id = Id;
        }

        public override void MapFrom(T model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Id = model.Id;
        }
    }
}