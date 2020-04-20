namespace nhitomi.Database
{
    public interface IDbModel<in T>
    {
        /// <summary>
        /// Maps this object to the specified object.
        /// </summary>
        void MapTo(T model);

        /// <summary>
        /// Maps the specified object to this object.
        /// </summary>
        void MapFrom(T model);
    }

    public abstract class DbModelBase<T> : IDbModel<T>
    {
        public virtual void MapTo(T model) { }
        public virtual void MapFrom(T model) { }

        public virtual void UpdateCache() { }
    }
}