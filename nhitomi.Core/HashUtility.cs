using System;
using System.IO;
using System.Security.Cryptography;

namespace nhitomi.Core
{
    public static class HashUtility
    {
        // SHA256 is NOT thread-safe!!
        // https://medium.com/@jamesikanos/c-cautionary-tail-the-dangers-of-sha256-reuse-2b5bb9c6fde9
        [ThreadStatic] static SHA256 _sha;

        public static byte[] GetSha256(byte[] buffer)
        {
            if (_sha == null)
                _sha = SHA256.Create();

            return _sha.ComputeHash(buffer);
        }

        public static byte[] GetSha256(Stream stream)
        {
            if (_sha == null)
                _sha = SHA256.Create();

            return _sha.ComputeHash(stream);
        }
    }
}