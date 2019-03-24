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
            double expiresIn = double.PositiveInfinity
        )
        {
            encoding = encoding ?? Encoding.UTF8;
            serializer = serializer ?? JsonSerializer.CreateDefault();

            // Create identity
            var payloadData = new DownloadTokenPayload
            {
                Source = doujin.Source.Name,
                Id = doujin.Id,
                Expires = double.IsInfinity(expiresIn)
                    ? (DateTime?) null
                    : DateTime.UtcNow.AddMinutes(expiresIn)
            };

            // Serialize payload
            string payload;

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, encoding))
            {
                serializer.Serialize(writer, payloadData);
                writer.Flush();

                payload = Convert.ToBase64String(stream.ToArray());
            }

            // Signature
            var signature = HashHelper.GetHash(payload, secret, encoding);

            // Token (similar to JWT, without header)
            return $"{payload}.{signature}";
        }

        public static bool TryDeserializeDownloadToken(
            string token,
            string secret,
            out string sourceName,
            out string id,
            Encoding encoding = null,
            JsonSerializer serializer = null,
            bool validateExpiry = true
        )
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                serializer = serializer ?? JsonSerializer.CreateDefault();

                // Get parts
                var payload = token.Substring(0, token.IndexOf('.'));
                var signature = token.Substring(token.IndexOf('.') + 1);

                // Verify signature
                if (signature != HashHelper.GetHash(payload, secret, encoding))
                {
                    sourceName = null;
                    id = null;
                    return false;
                }

                // Deserialize payload
                DownloadTokenPayload payloadData;

                using (var stream = new MemoryStream(Convert.FromBase64String(payload)))
                using (var streamReader = new StreamReader(stream, encoding))
                using (var jsonReader = new JsonTextReader(streamReader))
                    payloadData = serializer.Deserialize<DownloadTokenPayload>(jsonReader);

                // Test expiry time
                if (validateExpiry &&
                    DateTime.UtcNow >= payloadData.Expires)
                {
                    sourceName = null;
                    id = null;
                    return false;
                }

                sourceName = payloadData.Source;
                id = payloadData.Id;
                return true;
            }
            catch
            {
                sourceName = null;
                id = null;
                return false;
            }
        }
    }
}
