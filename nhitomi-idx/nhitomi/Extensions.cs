using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
using StackExchange.Redis;

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

    public static class Extensions
    {
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

        public static string ToStringSafe(this RedisKey key) => EscapeUnicode(key);

        public static string EscapeUnicode(this string str)
        {
            var builder = new StringBuilder();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];

                // ascii that can be displayed
                if (32 < c && c < 127)
                {
                    builder.Append(c);
                }
                else
                {
                    var encoded = "\\u" + ((int) c).ToString("x4");

                    builder.Append(encoded);
                }
            }

            return builder.ToString();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T2[] ToArray<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> selector)
            => enumerable.Select(selector).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T2[] ToArrayMany<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, IEnumerable<T2>> selector)
            => enumerable.SelectMany(selector).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T2> ToList<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> selector)
            => enumerable.Select(selector).ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T2> ToListMany<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, IEnumerable<T2>> selector)
            => enumerable.SelectMany(selector).ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable WrapInDisposable(this Action action) => new DelegatingDisposable(action);

        sealed class DelegatingDisposable : IDisposable
        {
            readonly Action _action;

            public DelegatingDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose() => _action?.Invoke();
        }

        // FROM: https://stackoverflow.com/a/48599119
        /// <summary>
        /// Compares this byte array to the specified byte array using <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BufferEquals(this byte[] a, byte[] b) => ((Span<byte>) a).SequenceEqual(b);

        /// <summary>
        /// Returns true if this array of byte arrays is a subset of another array of byte arrays.
        /// </summary>
        public static bool BufferSubset(this IEnumerable<byte[]> a, IEnumerable<byte[]> b)
            => new HashSet<byte[]>(a, new ByteArrayEqualityComparer()).IsSubsetOf(b);

        sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            readonly uint _seed = unchecked((uint) new Random().Next());

            public bool Equals(byte[] x, byte[] y) => ((Span<byte>) x).SequenceEqual(y);
            public int GetHashCode(byte[] obj) => unchecked((int) Crc32.Compute(_seed, obj));
        }

        /// <summary>
        /// Finds the next available TCP port by binding to it.
        /// </summary>
        public static int NextTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            try
            {
                return ((IPEndPoint) listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// A list that will dispose all <see cref="IDisposable"/> elements when it is being disposed.
        /// </summary>
        public class DisposableList<T> : List<T>, IDisposable
        {
            public void Dispose()
            {
                foreach (var item in this)
                {
                    if (item is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }
    }
}