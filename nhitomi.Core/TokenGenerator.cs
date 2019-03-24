// Copyright (c) 2018-2019 phosphene47
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
            var signature = HashHelper.GetHash(payload, secret, encoding);

            // Token (similar to JWT, without header)
            return $"{payload}.{signature}";
        }

        static DateTime? getExpirationFromNow(double? expireMinutes) =>
            expireMinutes == null ? (DateTime?) null : DateTime.UtcNow.AddMinutes(expireMinutes.Value);

        public struct ProxyTokenPayload
        {
            public string Url;
            public DateTime? Expires;
        }

        public static string CreateProxyToken(
            string url,
            string secret,
            Encoding encoding = null,
            JsonSerializer serializer = null,
            double? expireMinutes = null)
        {
            var payload = new ProxyTokenPayload
            {
                Url = url,
                Expires = getExpirationFromNow(expireMinutes)
            };

            return CreateToken(payload, secret, encoding, serializer);
        }

        public struct DownloadTokenPayload
        {
            public string Source;
            public string Id;
            public DateTime? Expires;
        }

        public static string CreateDownloadToken(
            this IDoujin doujin,
            string secret,
            Encoding encoding = null,
            JsonSerializer serializer = null,
            double? expireMinutes = null)
        {
            var payload = new DownloadTokenPayload
            {
                Source = doujin.Source.Name,
                Id = doujin.Id,
                Expires = getExpirationFromNow(expireMinutes)
            };

            return CreateToken(payload, secret, encoding, serializer);
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
                if (signaturePart != HashHelper.GetHash(payloadPart, secret, encoding))
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

        public static bool TryDeserializeDownloadToken(
            string token,
            string secret,
            out string sourceName,
            out string id,
            Encoding encoding = null,
            JsonSerializer serializer = null,
            bool validateExpiry = true)
        {
            sourceName = null;
            id = null;

            if (!TryDeserializeToken<DownloadTokenPayload>(token, secret, out var payload, encoding, serializer))
                return false;

            if (validateExpiry && DateTime.UtcNow >= payload.Expires)
                return false;

            sourceName = payload.Source;
            id = payload.Id;
            return true;
        }

        public static bool TryDeserializeProxyToken(
            string token,
            string secret,
            out string url,
            Encoding encoding = null,
            JsonSerializer serializer = null,
            bool validateExpiry = true)
        {
            url = null;

            if (!TryDeserializeToken<ProxyTokenPayload>(token, secret, out var payload, encoding, serializer))
                return false;

            if (validateExpiry && DateTime.UtcNow >= payload.Expires)
                return false;

            url = payload.Url;
            return true;
        }
    }
}