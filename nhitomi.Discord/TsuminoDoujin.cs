// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi
{
    public class TsuminoDoujin : IDoujin
    {
        readonly Tsumino.DoujinData _d;

        internal TsuminoDoujin(IDoujinClient client, Tsumino.DoujinData data)
        {
            Source = client;
            _d = data;
        }

        public string Id => _d.id.ToString();

        public string PrettyName
        {
            get
            {
                var parts = _d.title.Split('/', 2);

                return parts[0].Trim();
            }
        }
        public string OriginalName
        {
            get
            {
                var parts = _d.title.Split('/', 2);

                if (parts.Length == 1)
                    return parts[0].Trim();
                else
                    return parts[1].Trim();
            }
        }

        public DateTime UploadTime => DateTime.Parse(_d.uploaded);
        public DateTime ProcessTime => _d._processed;

        public IDoujinClient Source { get; }
        public string SourceUrl => $"https://www.tsumino.com/Book/Info/{Id}/";

        public string Scanlator => null;
        public string Language => "english";
        public string ParodyOf => _d.parody;

        public IEnumerable<string> Characters => _d.characters?.Select(convertTag);
        public IEnumerable<string> Categories => new[] { _d.category }.Select(convertTag).Where(c => c != "doujinshi");
        public IEnumerable<string> Artists => new[] { _d.artist }.Select(convertTag);
        public IEnumerable<string> Tags => _d.tags?.Select(convertTag);

        static string convertTag(string tag) => tag.ToLowerInvariant();

        public IEnumerable<string> PageUrls => _d.reader.reader_page_urls.Select(Tsumino.ImageObject);

        public override string ToString() => PrettyName;
    }
}