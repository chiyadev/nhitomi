using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace nhitomi
{
    /// <summary>
    /// Provides thread-safe hashing functions.
    /// </summary>
    public static class HashUtility
    {
        static readonly ConcurrentStack<SHA256CryptoServiceProvider> _sha256 = new ConcurrentStack<SHA256CryptoServiceProvider>();
        static readonly ConcurrentStack<SHA512CryptoServiceProvider> _sha512 = new ConcurrentStack<SHA512CryptoServiceProvider>();

        public static byte[] Sha256(byte[] buffer)
        {
            if (!_sha256.TryPop(out var sha256))
                sha256 = new SHA256CryptoServiceProvider();

            try
            {
                return sha256.ComputeHash(buffer);
            }
            finally
            {
                _sha256.Push(sha256);
            }
        }

        public static byte[] Sha512(byte[] buffer)
        {
            if (!_sha512.TryPop(out var sha512))
                sha512 = new SHA512CryptoServiceProvider();

            try
            {
                return sha512.ComputeHash(buffer);
            }
            finally
            {
                _sha512.Push(sha512);
            }
        }
    }
}