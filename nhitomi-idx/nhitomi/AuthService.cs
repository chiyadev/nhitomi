using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using nhitomi.Controllers;
using nhitomi.Database;
using OneOf;
using StackExchange.Redis;

namespace nhitomi
{
    public enum TokenValidationError
    {
        Missing,
        VerificationFailed,
        Expired,
        Invalidated
    }

    public interface IAuthService
    {
        Task<string> GenerateTokenAsync(DbUser user, CancellationToken cancellationToken = default);
        Task<byte[]> GetPayloadHashAsync(byte[] buffer, CancellationToken cancellationToken = default);

        Task<OneOf<AuthTokenPayload, TokenValidationError>> ValidateTokenAsync(string token);

        Task<long> GetSessionIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<long> InvalidateSessionAsync(string userId, CancellationToken cancellationToken = default);
    }

    public class AuthService : IAuthService
    {
        static readonly MessagePackSerializerOptions _serializerOptions = MessagePackSerializerOptions.Standard;

        readonly IOptionsMonitor<UserServiceOptions> _options;
        readonly IRedisClient _redis;

        public AuthService(IOptionsMonitor<UserServiceOptions> options, IRedisClient redis)
        {
            _options = options;
            _redis   = redis;
        }

        public async Task<string> GenerateTokenAsync(DbUser user, CancellationToken cancellationToken = default)
        {
            var payloadBuffer = MessagePackSerializer.Serialize(new AuthTokenPayload
            {
                UserId    = user.Id,
                Expiry    = DateTime.UtcNow + _options.CurrentValue.AccessTokenLifetime,
                SessionId = await GetSessionIdAsync(user.Id, cancellationToken)
            }, _serializerOptions);

            var hashBuffer = await GetPayloadHashAsync(payloadBuffer, cancellationToken);

            var payload = WebEncoders.Base64UrlEncode(payloadBuffer);
            var hash    = WebEncoders.Base64UrlEncode(hashBuffer);

            return $"{payload}.{hash}";
        }

        readonly ConcurrentQueue<HMACSHA256> _hashes = new ConcurrentQueue<HMACSHA256>();

        public async Task<byte[]> GetPayloadHashAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            // get or create new hash processor
            if (!_hashes.TryDequeue(out var hash))
                hash = new HMACSHA256(await GetSigningKeyAsync(cancellationToken));

            try
            {
                return hash.ComputeHash(buffer);
            }
            finally
            {
                // release hash processor for reuse
                _hashes.Enqueue(hash);
            }
        }

        public async Task<OneOf<AuthTokenPayload, TokenValidationError>> ValidateTokenAsync(string token)
        {
            var parts = token?.Split('.', 2);

            if (parts == null || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
                return TokenValidationError.Missing;

            var buffer = WebEncoders.Base64UrlDecode(parts[0]);

            // validate hash
            var hash1 = WebEncoders.Base64UrlDecode(parts[1]);
            var hash2 = await GetPayloadHashAsync(buffer);

            if (!hash1.BufferEquals(hash2))
                return TokenValidationError.VerificationFailed;

            // deserialize payload
            var payload = MessagePackSerializer.Deserialize<AuthTokenPayload>(buffer, _serializerOptions);

            // validation
            if (payload.Expiry <= DateTime.UtcNow)
                return TokenValidationError.Expired;

            if (payload.SessionId != await GetSessionIdAsync(payload.UserId))
                return TokenValidationError.Invalidated;

            return payload;
        }

        async ValueTask<byte[]> GetSigningKeyAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                // get key
                var key = await _redis.GetAsync("config:signKey", cancellationToken);

                // generate new key if null
                if (key == null)
                {
                    using (var rand = new RNGCryptoServiceProvider())
                    {
                        key = new byte[64]; // 64 is recommended https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256.-ctor
                        rand.GetBytes(key);
                    }

                    if (!await _redis.SetAsync("config:signKey", key, null, When.NotExists, cancellationToken))
                        continue;
                }

                return key;
            }
        }

        public Task<long> GetSessionIdAsync(string userId, CancellationToken cancellationToken = default)
            => _redis.GetIntegerAsync($"sess:{userId}", cancellationToken);

        public Task<long> InvalidateSessionAsync(string userId, CancellationToken cancellationToken = default)
            => _redis.IncrementIntegerAsync($"sess:{userId}", 1, cancellationToken);
    }
}