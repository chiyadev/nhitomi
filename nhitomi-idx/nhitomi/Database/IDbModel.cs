using System;

namespace nhitomi.Database
{
    /// <summary>
    /// Clarification:
    /// - Model is whatever that can exist in the database. This could be anything from an entire document to a nested object. It can map to a common model (DTO).
    /// - Object is a "model" that is specifically an individual document with an ID. It can also have cached properties that are used solely for querying.
    /// </summary>
    public interface IDbModel { }

    public interface IDbModel<in T> : IDbModel
    {
        /// <summary>
        /// Maps this object to the specified object.
        /// </summary>
        void MapTo(T model, IServiceProvider services);

        /// <summary>
        /// Maps the specified object to this object.
        /// </summary>
        void MapFrom(T model, IServiceProvider services);
    }

    public abstract class DbModelBase<T> : IDbModel<T>
    {
        public virtual void MapTo(T model, IServiceProvider services) { }
        public virtual void MapFrom(T model, IServiceProvider services) { }

        public virtual void UpdateCache(IServiceProvider services) { }
    }
}