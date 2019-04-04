// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nhitomi.Core.Clients;

namespace nhitomi.Core.Doujins
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
                var parts = _d.title.Split(new[] {'/'}, 2);

                return parts[0].Trim();
            }
        }

        public string OriginalName
        {
            get
            {
                var parts = _d.title.Split(new[] {'/'}, 2);

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
        public IEnumerable<string> Categories => new[] {_d.category}.Select(convertTag).Where(c => c != "doujinshi");
        public IEnumerable<string> Artists => new[] {_d.artist}.Select(convertTag);
        public IEnumerable<string> Tags => _d.tags?.Select(convertTag);

        static string convertTag(string tag) => tag.ToLowerInvariant();

        public int PageCount => _d.reader.reader_page_urls.Length;

        public IEnumerable<PageInfo> Pages => _d.reader.reader_page_urls.Select((i, index) => new PageInfo(
            index,
            Path.GetExtension(i),
            Tsumino.ImageObject(i)));

        public object GetSourceObject() => _d;

        public override string ToString() => PrettyName;
    }
}
