using System.Runtime.CompilerServices;
using Force.DeepCloner;

namespace nhitomi.Database
{
    public interface IDbModelConvertible<TThis, in TModel> : IDbModel<TModel>
        where TThis : IDbModelConvertible<TThis, TModel>
        where TModel : class, new() { }

    public interface IDbModelConvertible<TThis, in TModel, TBase> : IDbModelConvertible<TThis, TModel>
        where TThis : IDbModelConvertible<TThis, TModel, TBase>
        where TModel : class, TBase, new()
        where TBase : class, new() { }

    public static class DbModelConvertibleExtensions
    {
        /// <summary>
        /// Maps to a new <typeparamref name="TModel"/> and returns it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TModel Convert<TThis, TModel>(this IDbModelConvertible<TThis, TModel> obj)
            where TThis : IDbModelConvertible<TThis, TModel>
            where TModel : class, new()
            => new TModel().Chain(obj.MapTo);

        /// <summary>
        /// Calls <see cref="IDbModel{T}.MapFrom"/> and returns itself.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TThis Apply<TThis, TModel>(this IDbModelConvertible<TThis, TModel> obj, TModel model)
            where TThis : IDbModelConvertible<TThis, TModel>
            where TModel : class, new()
            => (TThis) obj.Chain(x => x.MapFrom(model));

        /// <summary>
        /// Calls <see cref="IDbModel{T}.MapFrom"/> and returns itself.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TThis ApplyBase<TThis, TModel, TBase>(this IDbModelConvertible<TThis, TModel, TBase> obj, TBase other)
            where TThis : IDbModelConvertible<TThis, TModel, TBase>
            where TModel : class, TBase, new()
            where TBase : class, new()
        {
            // convert db model to common model
            var model = obj.Convert();

            // apply base model to common model
            other.DeepCloneTo(model);

            // apply common model to db model
            obj.Apply(model);

            return (TThis) obj;
        }

        /// <summary>
        /// Applies the specified base model on this DB model by mapping to an intermediate full model, and returns whether there were changes or not.
        /// </summary>
        public static bool TryApplyBase<TThis, TModel, TBase>(this IDbModelConvertible<TThis, TModel, TBase> obj, TBase other)
            where TThis : IDbModelConvertible<TThis, TModel, TBase>
            where TModel : class, TBase, new()
            where TBase : class, new()
        {
            // convert db model to common model
            var model = obj.Convert();

            // apply base model to common model
            other.DeepCloneTo(model);

            // clone db model
            var clone = obj.DeepClone();

            // apply common model to db model
            obj.Apply(model);

            // compare current db model and previous db model for changes
            return !obj.DeepEqualTo(clone);
        }
    }
}