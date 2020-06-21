using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace nhitomi
{
    //FROM: https://stackoverflow.com/a/23391746
    /// <summary>
    /// Class to cast to type <typeparamref name="TTo"/>
    /// </summary>
    /// <typeparam name="TTo">Target type</typeparam>
    public static class CastTo<TTo>
    {
        /// <summary>
        /// Casts <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// This does not cause boxing for value types.
        /// Useful in generic methods.
        /// </summary>
        /// <typeparam name="TFrom">Source type to cast from. Usually a generic type.</typeparam>
        public static TTo Cast<TFrom>(TFrom s) => Cache<TFrom>.Caster(s);

        static class Cache<TFrom>
        {
            public static readonly Func<TFrom, TTo> Caster = Get();

            static Func<TFrom, TTo> Get()
            {
                var p = Expression.Parameter(typeof(TFrom));
                var c = Expression.ConvertChecked(p, typeof(TTo));
                return Expression.Lambda<Func<TFrom, TTo>>(c, p).Compile();
            }
        }
    }

    public static class HistogramEx
    {
        /// <summary>
        /// Similar to <see cref="Histogram.ExponentialBuckets"/> but takes a max value instead of the common ratio.
        /// </summary>
        public static double[] ExponentialBuckets(double min, double max, int count, int round = 2)
        {
            var r = Math.Pow(max / min, 1.0 / (count - 1));
            var a = new double[count];

            for (var i = 0; i < count; i++)
                a[i] = Math.Round(min * Math.Pow(r, i), round);

            return a;
        }
    }

    public enum ObservationUnits
    {
        Milliseconds = 0,
        Seconds = 1
    }

    public static class Extensions
    {
        sealed class HistogramMeasureContext : IDisposable
        {
            readonly MeasureContext _measure = new MeasureContext();
            readonly IObserver _observer;
            readonly ObservationUnits _units;

            public HistogramMeasureContext(IObserver observer, ObservationUnits units)
            {
                _observer = observer;
                _units    = units;
            }

            public void Dispose()
            {
                _measure.Dispose();

                switch (_units)
                {
                    case ObservationUnits.Milliseconds:
                        _observer.Observe(_measure.Milliseconds);
                        break;

                    case ObservationUnits.Seconds:
                        _observer.Observe(_measure.Seconds);
                        break;
                }
            }
        }

        /// <summary>
        /// Similar to <see cref="TimerExtensions.NewTimer(Prometheus.IObserver)"/> but observes the time in the specified unit.
        /// </summary>
        public static IDisposable Measure(this IObserver observer, ObservationUnits units = ObservationUnits.Milliseconds) => new HistogramMeasureContext(observer, units);

        sealed class DisposableStackContext<T> : IDisposable
        {
            readonly Stack<T> _stack;

            public DisposableStackContext(Stack<T> stack)
            {
                _stack = stack;
            }

            public void Dispose() => _stack.Pop();
        }

        /// <summary>
        /// Pushes an item to a stack and returns a disposable value that pops once when disposed.
        /// </summary>
        public static IDisposable PushContext<T>(this Stack<T> stack, T item)
        {
            stack.Push(item);
            return new DisposableStackContext<T>(stack);
        }

        sealed class DisposableSemaphoreReleaseContext : IDisposable
        {
            readonly SemaphoreSlim _semaphore;

            public DisposableSemaphoreReleaseContext(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose() => _semaphore.ReleaseSafe();
        }

        /// <summary>
        /// Waits on this semaphore and returns a value that can release on disposal.
        /// </summary>
        public static async Task<IDisposable> EnterAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new DisposableSemaphoreReleaseContext(semaphore);
        }

        /// <summary>
        /// Safely releases a semaphore handling <see cref="ObjectDisposedException"/>.
        /// </summary>
        public static void ReleaseSafe(this SemaphoreSlim semaphore)
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException) { }
        }

        static readonly CompareLogic _deepComparer = new CompareLogic(new ComparisonConfig
        {
            MaxDifferences    = 1,
            IgnoreObjectTypes = true
        });

        static readonly CompareLogic _shallowComparer = new CompareLogic(new ComparisonConfig
        {
            MaxDifferences    = 1,
            IgnoreObjectTypes = true,
            CompareChildren   = false
        });

        /// <summary>
        /// Compares this object to the specified object for equality considering the entire graph.
        /// </summary>
        public static bool DeepEqualTo<T1, T2>(this T1 obj, T2 other)
            => _deepComparer.Compare(obj, other).AreEqual;

        /// <summary>
        /// Compares this object to the specified object for equality.
        /// </summary>
        public static bool ShallowEqualTo<T1, T2>(this T1 obj, T2 other)
            => _shallowComparer.Compare(obj, other).AreEqual;

        /// <summary>
        /// Returns an array of individual distinct flags for the specified enum value.
        /// </summary>
        public static T[] ToFlags<T>(this T value) where T : Enum
            => EnumInfoContainer<T>.Flags.Where(f => value.HasFlag(f)).ToArray();

        /// <summary>
        /// Performs bitwise OR on the specified values and returns the result.
        /// This method assumes <typeparamref name="T"/> is convertible to int64.
        /// </summary>
        public static T ToBitwise<T>(this IEnumerable<T> values) where T : Enum
            => values == null ? default : CastTo<T>.Cast(values.Select(CastTo<long>.Cast).Aggregate(0L, (a, b) => a | b));

        /// <summary>
        /// Filters this collection of bitwise values and returns individual distinct flags.
        /// This is equivalent to calling <see cref="ToBitwise{T}"/> and then <see cref="ToFlags{T}"/>.
        /// </summary>
        public static T[] ToDistinctFlags<T>(this IEnumerable<T> values) where T : Enum
            => values.ToBitwise().ToFlags();

        static class EnumInfoContainer<T> where T : Enum
        {
            public static readonly List<T> Flags = new List<T>();

            //FROM: https://stackoverflow.com/a/22222260
            static EnumInfoContainer()
            {
                var flag = 1L;

                foreach (var value in Enum.GetValues(typeof(T)).Cast<T>())
                {
                    var bits = CastTo<long>.Cast(value);

                    while (flag < bits)
                        flag <<= 1;

                    if (flag == bits)
                        Flags.Add(value);
                }
            }
        }

        /// <summary>
        /// Returns the current object after performing some action, which allows you to perform operations in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Chain<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        /// <summary>
        /// Returns a task that returns the current object after performing some action asynchronously, which allows you to perform operations in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> Chain<T>(this T obj, Func<T, Task> action)
        {
            await action(obj);
            return obj;
        }

        /// <summary>
        /// Returns a task that unwraps the current task and returns the returned object after performing some action asynchronously, which allows you to perform operations in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> ChainAsync<T>(this Task<T> task, Func<T, Task> action)
        {
            var result = await task;

            await action(result);
            return result;
        }

        /// <summary>
        /// Returns the return value of the given action, which allows you to compose functions in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T2 Compose<T1, T2>(this T1 obj, Func<T1, T2> action) => action(obj);

        /// <summary>
        /// Returns a task that the return value of the given asynchronous action, which allows you to compose functions in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T2> Compose<T1, T2>(this T1 obj, Func<T1, Task<T2>> action) => action(obj);

        /// <summary>
        /// Returns a task that unwraps the current task and returns the return value of the given asynchronous action, which allows you to compose functions in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T2> ComposeAsync<T1, T2>(this Task<T1> task, Func<T1, T2> action) => action(await task);

        /// <summary>
        /// Returns a task that unwraps the current task and returns the return value of the given asynchronous action, which allows you to compose functions in a "fluent" manner.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T2> ComposeAsync<T1, T2>(this Task<T1> task, Func<T1, Task<T2>> action) => await action(await task);

        public static StringBuilder AppendException(this StringBuilder builder, Exception e, bool trimTrace = false)
        {
            if (e == null)
                return builder;

            // add message
            if (!string.IsNullOrEmpty(e.Message))
                builder.AppendLine(e.Message);

            var trace = e.StackTrace;

            if (trimTrace)
                trace = e.StackTrace.Substring(0, e.StackTrace.IndexOf('\n')).Trim();

            return builder.AppendLine(trace);
        }

        public static string ToStringWithTrace(this Exception e, string message = null, bool trimTrace = false)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(message))
                builder.AppendLine(message);

            return builder.AppendException(e, trimTrace).ToString().Trim();
        }

        public static async Task<MemoryStream> AsMemoryAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memory)
                return memory;

            return await stream.ToMemoryAsync(cancellationToken);
        }

        public static async Task<MemoryStream> ToMemoryAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            var memory = stream.CanSeek ? new MemoryStream((int) stream.Length) : new MemoryStream();

            try
            {
                await stream.CopyToAsync(memory, cancellationToken);

                return memory;
            }
            catch
            {
                memory.Dispose();
                throw;
            }
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memory)
                return memory.ToArray();

            await using (memory = await stream.ToMemoryAsync(cancellationToken))
                return memory.ToArray();
        }

        // https://kristian.hellang.com/using-mvc-result-executors-in-middleware/
        static readonly RouteData _emptyRouteData = new RouteData();
        static readonly ActionDescriptor _emptyActionDescriptor = new ActionDescriptor();

        public static Task ExecuteResultAsync<TResult>(this TResult result, HttpContext context) where TResult : IActionResult
        {
            var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<TResult>>();

            var routeData     = context.GetRouteData() ?? _emptyRouteData;
            var actionContext = new ActionContext(context, routeData, _emptyActionDescriptor);

            return executor.ExecuteAsync(actionContext, result);
        }

        public static T[] ToArray<T>(this ISet<T> set)
        {
            var array = new T[set.Count];
            set.CopyTo(array, 0);
            return array;
        }

        // ReSharper disable PossibleMultipleEnumeration
        public static T2[] ToArray<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> selector)
        {
            var length = enumerable switch
            {
                T1[] c                    => c.Length,
                ICollection<T1> c         => c.Count,
                IReadOnlyCollection<T1> c => c.Count,

                _ => null as int?
            };

            if (length == null)
                return enumerable.Select(selector).ToArray();

            var array = new T2[length.Value];
            var count = 0;

            foreach (var item in enumerable)
                array[count++] = selector(item);

            return array;
        }
        // ReSharper enable PossibleMultipleEnumeration

        public static T2[] ToArrayMany<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, IEnumerable<T2>> selector)
            => enumerable.SelectMany(selector).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T2> ToList<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> selector)
            => enumerable.Select(selector).ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T2> ToListMany<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, IEnumerable<T2>> selector)
            => enumerable.SelectMany(selector).ToList();

        /// <summary>
        /// Merges this collection with another collection, ignoring duplicate elements.
        /// This will not throw with null arguments.
        /// </summary>
        public static T[] DistinctMergeSafe<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
        {
            if (enumerable == null)
                return other?.ToArray();

            if (other == null)
                return enumerable.ToArray();

            return enumerable.Concat(other).Distinct().ToArray();
        }

        /// <summary>
        /// Merges this dictionary with another dictionary, ignoring duplicate elements.
        /// This will not throw with null arguments.
        /// Left-hand dictionary will be modified in-place.
        /// </summary>
        public static Dictionary<TKey, TValue[]> DistinctMergeSafe<TKey, TValue>(this Dictionary<TKey, TValue[]> dict, Dictionary<TKey, TValue[]> other)
        {
            if (dict == null)
                return other;

            if (other == null)
                return dict;

            foreach (var (key, values) in other)
            {
                if (dict.TryGetValue(key, out var existing))
                    dict[key] = existing.DistinctMergeSafe(values);
                else
                    dict[key] = values;
            }

            return dict;
        }

        /// <summary>
        /// Performs a semi-shallow clone of this dictionary by cloning the array but not its elements.
        /// </summary>
        public static Dictionary<TKey, TValue[]> DictClone<TKey, TValue>(this Dictionary<TKey, TValue[]> dict)
        {
            var other = new Dictionary<TKey, TValue[]>(dict.Count);

            foreach (var (key, values) in dict)
                other[key] = values?.ToArray();

            return other;
        }

        // FROM: https://stackoverflow.com/a/48599119
        /// <summary>
        /// Compares this byte array to the specified byte array using <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BufferEquals(this byte[] a, byte[] b) => ((Span<byte>) a).SequenceEqual(b);

        /// <summary>
        /// Returns the name of this enum specified by <see cref="EnumMemberAttribute"/>.
        /// This does not handle bitwise enums.
        /// </summary>
        public static string GetEnumName<T>(this T value) where T : struct, Enum
            => value.GetType()
                    .GetMember(value.ToString())[0]
                    .GetCustomAttribute<EnumMemberAttribute>()
                   ?.Value
            ?? value.ToString();
    }
}