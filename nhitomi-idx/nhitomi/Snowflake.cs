using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

// ReSharper disable All

namespace ChiyaFlake
{
    // ChiyaFlake by **chiya.dev**
    // GitHub: https://github.com/chiyadev/ChiyaFlake

    /// <summary><a href="https://github.com/chiyadev/ChiyaFlake">ChiyaFlake</a> static snowflake generator.</summary>
    public static class Snowflake
    {
        static int _offset = RandomNumberGenerator.GetInt32(int.MaxValue);
        static readonly ISnowflake[] _instances = new ISnowflake[64];

        static Snowflake()
        {
            for (var i = 0; i < _instances.Length; i++)
                _instances[i] = new SnowflakeInstance(i);
        }

        /// <summary><see cref="ISnowflake"/> instance used to generate values.</summary>
        public static ISnowflake Current => _instances[Math.Abs(Interlocked.Increment(ref _offset) % _instances.Length)];

        /// <inheritdoc cref="ISnowflake.Timestamp"/>
        public static long Timestamp => Current.Timestamp;

        /// <inheritdoc cref="ISnowflake.New"/>
        public static string New => Current.New;

        /// <inheritdoc cref="ISnowflake.IsValid"/>
        public static bool IsValid(string value) => Current.IsValid(value);

        /// <summary>Defines the maximum possible length of snowflake strings.</summary>
        public static int MaxLength { get; } = (new MaxSnowflake() as ISnowflake).New.Length;

        sealed class MaxSnowflake : ISnowflake
        {
            long ISnowflake.Timestamp => long.MaxValue;
        }
    }

    /// <summary><a href="https://github.com/chiyadev/ChiyaFlake">ChiyaFlake</a> snowflake generator interface.</summary>
    public interface ISnowflake
    {
        /// <summary>Gets the timestamp value used for snowflake generation.</summary>
        long Timestamp { get; }

        /// <summary>Gets a short base64-encoded URL-safe snowflake string generated using <see cref="Timestamp"/>.</summary>
        string New
        {
            get
            {
                var buffer = BitConverter.GetBytes(Timestamp);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(buffer);

                var offset = Array.FindIndex(buffer, x => x != 0);

                return Convert.ToBase64String(buffer, offset, buffer.Length - offset).TrimEnd('=').Replace("/", "_").Replace("+", "-");
            }
        }

        /// <summary>Determines whether the given value is a valid snowflake string.</summary>
        /// <param name="value">Snowflake string.</param>
        /// <returns>Whether <paramref name="value"/> is valid.</returns>
        bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > Snowflake.MaxLength)
                return false;

            foreach (var c in value)
            {
                // url-safe base64
                if ('A' <= c && c <= 'Z' ||
                    'a' <= c && c <= 'z' ||
                    '0' <= c && c <= '9' ||
                    '_' == c ||
                    '-' == c)
                    continue;

                return false;
            }

            return true;
        }
    }

    /// <summary><a href="https://github.com/chiyadev/ChiyaFlake">ChiyaFlake</a> snowflake generator instance.</summary>
    public sealed class SnowflakeInstance : ISnowflake
    {
        readonly long _id;
        readonly TimeSpan _offset;
        readonly Stopwatch _watch = Stopwatch.StartNew();

        /// <summary>Initializes a new <see cref="Snowflake"/> instance with generator ID and epoch time.</summary>
        /// <param name="id">6-bit generator ID [0, 64), or null to use a cryptographically secure random ID.</param>
        /// <param name="epoch">Epoch time, or null to use 2000/01/01.</param>
        public SnowflakeInstance(int? id = null, DateTimeOffset? epoch = null)
        {
            id    ??= RandomNumberGenerator.GetInt32(64);
            epoch ??= new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (id.Value >= 64)
                throw new ArgumentOutOfRangeException(nameof(id), id, $"{nameof(id)} must be in the range [0, 63].");

            _id     = id.Value;
            _offset = DateTime.UtcNow - epoch.Value.ToUniversalTime();
        }

        long _lastTimestamp;

        /// <inheritdoc cref="ISnowflake.Timestamp"/>
        public long Timestamp
        {
            get
            {
                long original, current;

                do
                {
                    var now = (long) (_offset + _watch.Elapsed).TotalMilliseconds;

                    original = _lastTimestamp;
                    current  = Math.Max(now, original + 1);
                }
                while (Interlocked.CompareExchange(ref _lastTimestamp, current, original) > original);

                return (current << 6) | _id;
            }
        }
    }
}