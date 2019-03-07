// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Core
{
    public sealed class HitomiDoujin : IDoujin
    {
        readonly Hitomi.DoujinData _d;

        internal HitomiDoujin(HitomiClient client, Hitomi.DoujinData data)
        {
            Source = client;
            _d = data;
        }

        public string Id => _d.id.ToString();

        public string PrettyName => _d.name;
        public string OriginalName => _d.name;

        public DateTime UploadTime => DateTime.Parse(_d.date);
        public DateTime ProcessTime => _d._processed;

        public IDoujinClient Source { get; }
        public string SourceUrl => $"https://hitomi.la/galleries/{Id}.html";

        public string Scanlator => null;
        public string Language => convertLanguage(_d.language);
        public string ParodyOf => _d.series == "original" ? null : _d.series;

        static string convertLanguage(string lang)
        {
            switch (lang)
            {
                case "日本語":
                    return "japanese";
                case "한국어":
                    return "korean";
                case "中文":
                    return "chinese";
                default:
                    return lang;
            }
        }

        public IEnumerable<string> Characters => _d.characters;
        public IEnumerable<string> Categories => null;
        public IEnumerable<string> Artists => _d.artists;
        public IEnumerable<string> Tags => _d.tags?.Select(t => t.Value);

        public IEnumerable<string> PageUrls => _d.images?.Select(i => Hitomi.Image(_d.id, i.name));

        public override string ToString() => PrettyName;
    }
}