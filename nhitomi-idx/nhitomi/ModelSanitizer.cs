using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using FastExpressionCompiler;

namespace nhitomi
{
    /// <summary>
    /// Note: model sanitizer does not handle circular references in runtime.
    /// This is fine because this sanitizer is only used in the model binder pipeline, which should never generate circularly referencing values.
    /// </summary>
    public static class ModelSanitizer
    {
        static readonly ConcurrentDictionary<Type, IModelSanitizer> _cache = new ConcurrentDictionary<Type, IModelSanitizer>
        {
            [typeof(object)]   = new EmptySanitizer<object>(),
            [typeof(object[])] = new EmptySanitizer<object[]>(),
            [typeof(string)]   = new StringSanitizer(),
            [typeof(bool)]     = new EmptySanitizer<bool>(),
            [typeof(int)]      = new EmptySanitizer<int>(),
            [typeof(byte[])]   = new EmptySanitizer<byte[]>()
        };

        public static T Sanitize<T>(T value) => GetSanitizer<T>().Sanitize(value);

        public static object Sanitize(object value)
        {
            if (value is null)
                return null;

            return GetSanitizer(value.GetType()).SanitizeNonGeneric(value);
        }

        public static IModelSanitizer GetSanitizer(Type type)
        {
            if (_cache.TryGetValue(type, out var cached))
                return cached;

            return _cache[type] = BuildSanitizer(type);
        }

        public static IModelSanitizer<T> GetSanitizer<T>() => (IModelSanitizer<T>) GetSanitizer(typeof(T));

        static readonly ThreadLocal<Stack<Type>> _sanitizerInitStack = new ThreadLocal<Stack<Type>>(() => new Stack<Type>());

        public static IModelSanitizer BuildSanitizer(Type type)
        {
            Type sanitizerType;

            var stack = _sanitizerInitStack.Value;

            // the type for which we are creating a sanitizer has circular reference,
            // but the sanitizer instance is not added to the dictionary until construction is complete.
            // we use a proxy that will lazily retrieve the sanitizer once it is added.
            if (stack.Contains(type))
            {
                sanitizerType = typeof(ProxiedSanitizer<>).MakeGenericType(type);
            }

            else
            {
                sanitizerType = GetSanitizerType(type);

                if (sanitizerType == null)
                    throw new Exception($"Could not build sanitizer for {type}.");

                // if the sanitizer does not strictly implement the interface for the given type, wrap in casting sanitizer
                if (!typeof(IModelSanitizer<>).MakeGenericType(type).IsAssignableFrom(sanitizerType))
                {
                    var innerType = sanitizerType.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(IModelSanitizer<>)).GetGenericArguments()[0];

                    sanitizerType = typeof(CastingSanitizer<,>).MakeGenericType(type, innerType);
                }
            }

            stack.Push(type);

            try
            {
                return Activator.CreateInstance(sanitizerType) as IModelSanitizer;
            }
            finally
            {
                stack.Pop();
            }
        }

        static Type GetSanitizerType(Type type)
        {
            // ignored
            if (type.IsDefined(typeof(SanitizerIgnoreAttribute), true))
                return typeof(EmptySanitizer<>).MakeGenericType(type);

            // array
            if (type.IsArray)
                return typeof(ArraySanitizer<>).MakeGenericType(type.GetElementType());

            // enum
            if (type.IsEnum)
            {
                var flags = type.IsDefined(typeof(FlagsAttribute), true);

                return flags
                    ? typeof(BitwiseEnumSanitizer<>).MakeGenericType(type)
                    : typeof(EnumSanitizer<>).MakeGenericType(type);
            }

            // nullable
            var nullableUnderlying = Nullable.GetUnderlyingType(type);

            if (nullableUnderlying != null)
                return typeof(NullableSanitizer<>).MakeGenericType(nullableUnderlying);

            if (type.IsConstructedGenericType)
            {
                var typeArgs = type.GetGenericArguments();

                switch (typeArgs.Length)
                {
                    case 1:
                    {
                        // dictionary that looks like a list of kvp
                        if (typeArgs[0].IsConstructedGenericType && typeArgs[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        {
                            var dict = typeof(ICollection<>).MakeGenericType(typeArgs[0]);

                            if (dict.IsAssignableFrom(type))
                                return typeof(DictionarySanitizer<,>).MakeGenericType(typeArgs[0].GetGenericArguments());
                        }

                        // list
                        var list = typeof(ICollection<>).MakeGenericType(typeArgs);

                        if (list.IsAssignableFrom(type))
                            return typeof(ListSanitizer<>).MakeGenericType(typeArgs);

                        break;
                    }

                    case 2:
                    {
                        // dictionary
                        var dict = typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(typeArgs));

                        if (dict.IsAssignableFrom(type))
                            return typeof(DictionarySanitizer<,>).MakeGenericType(typeArgs);

                        break;
                    }
                }
            }

            // complex type
            if (type.IsClass)
                return typeof(ComplexTypeSanitizer<>).MakeGenericType(type);

            // struct (ignored)
            if (type.IsValueType)
                return typeof(EmptySanitizer<>).MakeGenericType(type);

            return null;
        }
    }

    public interface IModelSanitizer
    {
        object SanitizeNonGeneric(object value);
    }

    public interface IModelSanitizer<T> : IModelSanitizer
    {
        T Sanitize(T value);
    }

    public abstract class SanitizerBase<T> : IModelSanitizer<T>
    {
        public abstract T Sanitize(T value);
        public virtual object SanitizeNonGeneric(object value) => value is T v ? Sanitize(v) : value;
    }

    public interface IEmptySanitizer : IModelSanitizer { }

    public class EmptySanitizer<T> : SanitizerBase<T>, IEmptySanitizer
    {
        public override T Sanitize(T value) => value;
        public override object SanitizeNonGeneric(object value) => value;
    }

    public class ProxiedSanitizer<T> : SanitizerBase<T>
    {
        IModelSanitizer<T> _sanitizer;

        public override T Sanitize(T value)
        {
            if (_sanitizer == null)
                _sanitizer = ModelSanitizer.GetSanitizer<T>();

            return _sanitizer.Sanitize(value);
        }
    }

    public class CastingSanitizer<TOuter, TInner> : SanitizerBase<TOuter>
    {
        readonly Func<TOuter, TOuter> _sanitize;

        public CastingSanitizer()
        {
            var sanitizer       = ModelSanitizer.GetSanitizer<TInner>();
            var sanitizerMethod = sanitizer.GetType().GetMethod(nameof(Sanitize));

            var param = Expression.Parameter(typeof(TOuter), "value");

            // ReSharper disable once AssignNullToNotNullAttribute
            var result = Expression.Convert(Expression.Call(Expression.Constant(sanitizer), sanitizerMethod, Expression.Convert(param, typeof(TInner))), typeof(TOuter));

            _sanitize = Expression.Lambda<Func<TOuter, TOuter>>(result, param).CompileFast();
        }

        public override TOuter Sanitize(TOuter value) => _sanitize(value);
    }

    public class StringSanitizer : SanitizerBase<string>
    {
        // https://www.regular-expressions.info/unicode.html
        static readonly Regex _spaceRegex = new Regex(@"\p{Z}+", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        static readonly Regex _controlRegex = new Regex(@"\p{C}", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public override string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // replace spaces with " "
            value = _spaceRegex.Replace(value, " ");

            // remove control characters
            value = _controlRegex.Replace(value, "");

            // trim whitespaces
            value = value.Trim();

            // return null if empty
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    public class ArraySanitizer<T> : SanitizerBase<T[]>
    {
        readonly IModelSanitizer<T> _sanitizer = ModelSanitizer.GetSanitizer<T>();

        public override T[] Sanitize(T[] value)
        {
            if (value == null || value.Length == 0)
                return null;

            var list = new List<T>(value.Length);

            foreach (var item in value)
            {
                var sanitized = _sanitizer.Sanitize(item);

                if (sanitized is null)
                    continue;

                list.Add(sanitized);
            }

            if (list.Count == 0)
                return null;

            return list.ToArray();
        }
    }

    public class ListSanitizer<T> : SanitizerBase<ICollection<T>>
    {
        readonly IModelSanitizer<T> _sanitizer = ModelSanitizer.GetSanitizer<T>();

        public override ICollection<T> Sanitize(ICollection<T> value)
        {
            if (value == null || value.Count == 0)
                return null;

            var list = new List<T>(value.Count);

            foreach (var item in value)
            {
                var sanitized = _sanitizer.Sanitize(item);

                if (sanitized is null)
                    continue;

                list.Add(sanitized);
            }

            if (list.Count == 0)
                return null;

            value.Clear();

            foreach (var item in list)
                value.Add(item);

            return value;
        }
    }

    public class DictionarySanitizer<TKey, TValue> : SanitizerBase<ICollection<KeyValuePair<TKey, TValue>>>
    {
        readonly IModelSanitizer<TKey> _keySanitizer = ModelSanitizer.GetSanitizer<TKey>();
        readonly IModelSanitizer<TValue> _valueSanitizer = ModelSanitizer.GetSanitizer<TValue>();

        public override ICollection<KeyValuePair<TKey, TValue>> Sanitize(ICollection<KeyValuePair<TKey, TValue>> value)
        {
            if (value == null || value.Count == 0)
                return null;

            var dict = new Dictionary<TKey, TValue>(value.Count);

            foreach (var (k, v) in value)
            {
                var sanitizedKey = _keySanitizer.Sanitize(k);

                if (sanitizedKey is null || dict.ContainsKey(sanitizedKey))
                    continue;

                var sanitizedValue = _valueSanitizer.Sanitize(v);

                if (sanitizedValue is null)
                    continue;

                dict[sanitizedKey] = sanitizedValue;
            }

            if (dict.Count == 0)
                return null;

            value.Clear();

            foreach (var (k, v) in dict)
                value.Add(new KeyValuePair<TKey, TValue>(k, v));

            return value;
        }
    }

    public class NullableSanitizer<T> : SanitizerBase<T?> where T : struct
    {
        readonly IModelSanitizer<T> _sanitizer = ModelSanitizer.GetSanitizer<T>();

        public override T? Sanitize(T? value) => value is null ? null as T? : _sanitizer.Sanitize(value.Value);
        public override object SanitizeNonGeneric(object value) => value is T v ? _sanitizer.Sanitize(v) : null as T?;
    }

    public class EnumSanitizer<T> : SanitizerBase<T> where T : Enum
    {
        public override T Sanitize(T value) => Enum.IsDefined(typeof(T), value) ? value : default;
    }

    public class BitwiseEnumSanitizer<T> : SanitizerBase<T> where T : Enum
    {
        public override T Sanitize(T value) => value.ToFlags().ToBitwise(); // this will remove all undefined flags
    }

    /// <summary>
    /// Similar to <see cref="IValidatableObject"/> but for sanitization.
    /// </summary>
    public interface ISanitizableObject
    {
        /// <summary>
        /// Called immediately before this object is sanitized.
        /// </summary>
        void BeforeSanitize();

        /// <summary>
        /// Called immediately after this object is sanitized.
        /// </summary>
        void AfterSanitize();
    }

    /// <summary>
    /// Marks a class or property as ignored by <see cref="ModelSanitizer"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class SanitizerIgnoreAttribute : Attribute { }

    public class ComplexTypeSanitizer<T> : SanitizerBase<T> where T : class
    {
        readonly Func<T, T> _sanitize;

        public ComplexTypeSanitizer()
        {
            var type  = typeof(T);
            var param = Expression.Parameter(type, "value");

            var body = type.GetProperties()
                           .Where(p => p.CanRead && p.CanWrite)
                           .Select(p =>
                            {
                                if (p.IsDefined(typeof(SanitizerIgnoreAttribute), true))
                                    return null;

                                var sanitizer = ModelSanitizer.GetSanitizer(p.PropertyType);

                                if (sanitizer is IEmptySanitizer)
                                    return null;

                                var sanitizerMethod = sanitizer.GetType().GetMethod(nameof(Sanitize));

                                var property = Expression.Property(param, p);

                                // ReSharper disable once AssignNullToNotNullAttribute
                                return Expression.Assign(property, Expression.Call(Expression.Constant(sanitizer), sanitizerMethod, property)) as Expression;
                            })
                           .Where(x => x != null)
                           .ToList();

            // sanitize callback
            if (typeof(ISanitizableObject).IsAssignableFrom(type))
            {
                var before = type.GetMethod(nameof(ISanitizableObject.BeforeSanitize));
                var after  = type.GetMethod(nameof(ISanitizableObject.AfterSanitize));

                if (before != null)
                    body.Insert(0, Expression.Call(param, before));

                if (after != null)
                    body.Add(Expression.Call(param, after));
            }

            // return value
            body.Add(param);

            _sanitize = body.Count == 0
                ? v => v
                : Expression.Lambda<Func<T, T>>(Expression.Block(body), param).CompileFast();
        }

        public override T Sanitize(T value) => value is null ? null : _sanitize(value);
    }
}