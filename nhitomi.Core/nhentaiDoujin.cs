// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Core
{
    public sealed class nhentaiDoujin : IDoujin
    {
        readonly nhentai.DoujinData _d;

        internal nhentaiDoujin(IDoujinClient client, nhentai.DoujinData data)
        {
            Source = client;
            _d = data;
        }

        public string Id => _d.id.ToString();

        public string PrettyName => _d.title.pretty;
        public string OriginalName => _d.title.japanese;

        public DateTime UploadTime => DateTimeOffset.FromUnixTimeSeconds(_d.upload_date).UtcDateTime;
        public DateTime ProcessTime => _d._processed;

        public IDoujinClient Source { get; }
        public string SourceUrl => $"https://nhentai.net/g/{Id}/";

        public string Scanlator => string.IsNullOrWhiteSpace(_d.scanlator) ? null : _d.scanlator;
        public string Language => _d.tags?.FirstOrDefault(t => t.type == "language" && t.name != "translated").name;
        public string ParodyOf => _d.tags?.FirstOrDefault(t => t.type == "parody" && t.name != "original").name;

        public IEnumerable<string> Characters =>
            _d.tags?.Where(t => t.type == "character").Select(t => t.name).NullIfEmpty();

        public IEnumerable<string> Categories => _d.tags?.Where(t => t.type == "category" && t.name != "doujinshi")
            .Select(t => t.name).NullIfEmpty();

        public IEnumerable<string> Artists => _d.tags?.Where(t => t.type == "artist").Select(t => t.name).NullIfEmpty();
        public IEnumerable<string> Tags => _d.tags?.Where(t => t.type == "tag").Select(t => t.name).NullIfEmpty();

        static string expandExtension(string ext) => ext[0] == 'p' ? "png" : "jpg";

        public int PageCount => _d.images.pages.Length;

        public IEnumerable<PageInfo> Pages => _d.images.pages.Select((i, index) => new PageInfo(
            index,
            "." + expandExtension(i.t),
            nhentai.Image(_d.media_id, index, expandExtension(i.t))));

        public object GetSourceObject() => _d;

        public override string ToString() => PrettyName;
    }
}
