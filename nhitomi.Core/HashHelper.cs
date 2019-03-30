using System;
using System.Security.Cryptography;
using System.Text;

namespace nhitomi.Core
{
    public static class HashHelper
    {
        static readonly SHA256 _sha256 = System.Security.Cryptography.SHA256.Create();

        public static string SHA256(string data, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.Default;

            return Convert.ToBase64String(_sha256.ComputeHash(encoding.GetBytes(data)));
        }

        public static string HMACSHA256(string data, string secret, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.Default;

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