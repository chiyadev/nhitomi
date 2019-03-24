using System;
using System.Security.Cryptography;
using System.Text;

namespace nhitomi.Core
{
    public static class HashHelper
    {
        public static string GetHash(string data, string secret, Encoding encoding)
        {
            using (var hmac = new HMACSHA256(encoding.GetBytes(secret)))
            {
                return Convert
                    .ToBase64String(hmac.ComputeHash(encoding.GetBytes(data)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
