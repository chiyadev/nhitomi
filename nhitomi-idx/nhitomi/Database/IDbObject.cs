using MessagePack;
using Nest;

namespace nhitomi.Database
{
    public interface IDbObject : INanokaObject
    {
        /// <summary>
        /// Updates cached properties used for querying.
        /// </summary>
        void UpdateCache();
    }

    public interface IDbObject<in T> : IDbObject, IDbModel<T> where T : INanokaObject { }

    public abstract class DbObjectBase<T> : DbModelBase<T>, IDbObject<T> where T : INanokaObject
    {
        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("id"), Keyword(Name = "id", Index = false)]
        public string Id { get; set; }

        public override void MapTo(T model)
        {
            base.MapTo(model);

            model.Id = Id;
        }

        public override void MapFrom(T model)
        {
            base.MapFrom(model);

            Id = model.Id;
        }
    }
}