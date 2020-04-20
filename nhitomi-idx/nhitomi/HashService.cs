using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace nhitomi
{
    /// <summary>
    /// Used for hashing passwords.
    /// </summary>
    public interface IHashService
    {
        string Hash(string password);
        bool Test(string password, string hash);
    }

    public class HashService : IHashService
    {
        readonly ILogger<HashService> _logger;

        public HashService(ILogger<HashService> logger)
        {
            _logger = logger;
        }

        const int _currentVersion = 0;

        public string Hash(string password)
        {
            password ??= "";

            try
            {
                using var memory = new MemoryStream();
                using var writer = new BinaryWriter(memory);

                writer.Write(_currentVersion);

                switch (_currentVersion)
                {
                    case 0:
                        Hash0(writer, password);
                        break;
                }

                writer.Flush();

                return WebEncoders.Base64UrlEncode(memory.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception while hashing password.");
                return null;
            }
        }

        public bool Test(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            password ??= "";

            try
            {
                using var memory = new MemoryStream(WebEncoders.Base64UrlDecode(hash));
                using var reader = new BinaryReader(memory);

                var version = reader.ReadInt32();

                switch (version)
                {
                    case 0: return Test0(reader, password);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception while testing password.");
            }

            return false;
        }

#region v0

        static void Hash0(BinaryWriter writer, string password)
        {
            // salt
            var salt = new byte[128 / 8];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            writer.Write(salt.Length);
            writer.Write(salt);

            // hash
            var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, iterationCount: 10000, 256 / 8);

            writer.Write(hash.Length);
            writer.Write(hash);
        }

        static bool Test0(BinaryReader reader, string password)
        {
            // salt
            var len  = reader.ReadInt32();
            var salt = reader.ReadBytes(len);

            // hash
            len = reader.ReadInt32();

            var hash0 = reader.ReadBytes(len);
            var hash1 = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA1, iterationCount: 10000, 256 / 8);

            // test
            return ByteArrayCompare(hash0, hash1);
        }

#endregion

        // https://stackoverflow.com/a/48599119
        static bool ByteArrayCompare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) => x.SequenceEqual(y);
    }
}