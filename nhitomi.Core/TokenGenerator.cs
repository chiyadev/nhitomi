// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace nhitomi.Core
{
    public static class TokenGenerator
    {
        public static DateTime? GetExpirationFromNow(double? expireMinutes) =>
            expireMinutes == null ? (DateTime?) null : DateTime.UtcNow.AddMinutes(expireMinutes.Value);

        public static string CreateToken(
            object token,
            string secret,
            Encoding encoding = null,
            JsonSerializer serializer = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            serializer = serializer ?? JsonSerializer.CreateDefault();

            // Serialize payload
            string payload;

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, encoding))
            {
                serializer.Serialize(writer, token);
                writer.Flush();

                payload = Convert.ToBase64String(stream.ToArray());
            }

            // Signature
            var signature = HashHelper.HMACSHA256(payload, secret, encoding);

            // Token (similar to JWT, without header)
            return $"{payload}.{signature}";
        }

        public static bool TryDeserializeToken<TPayload>(
            string token,
            string secret,
            out TPayload payload,
            Encoding encoding = null,
            JsonSerializer serializer = null)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                serializer = serializer ?? JsonSerializer.CreateDefault();

                // Get parts
                var payloadPart = token.Substring(0, token.IndexOf('.'));
                var signaturePart = token.Substring(token.IndexOf('.') + 1);

                // Verify signature
                if (signaturePart != HashHelper.HMACSHA256(payloadPart, secret, encoding))
                {
                    payload = default;
                    return false;
                }

                // Deserialize payload
                using (var stream = new MemoryStream(Convert.FromBase64String(payloadPart)))
                using (var streamReader = new StreamReader(stream, encoding))
                using (var jsonReader = new JsonTextReader(streamReader))
                    payload = serializer.Deserialize<TPayload>(jsonReader);

                return true;
            }
            catch
            {
                payload = default;
                return false;
            }
        }

        public struct ProxyGetPayload
        {
            [JsonProperty("u")] public string Url;
            [JsonProperty("c")] public bool IsCached;
        }

        public struct ProxySetCachePayload
        {
            [JsonProperty("u")] public string Url;
        }

        public struct ProxyDownloadPayload
        {
            [JsonProperty("s")] public string Source;
            [JsonProperty("id")] public string Id;
            [JsonProperty("ri")] public double RequestThrottle;
            [JsonProperty("e")] public DateTime? Expires;
        }

        public struct ProxyRegistrationPayload
        {
            [JsonProperty("p")] public string ProxyUrl;
        }
    }
}