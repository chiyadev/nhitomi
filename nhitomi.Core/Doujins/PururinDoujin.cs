// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using nhitomi.Core.Clients;

namespace nhitomi.Core.Doujins
{
    public class PururinDoujin : IDoujin
    {
        readonly Pururin.DoujinData _d;

        internal PururinDoujin(IDoujinClient client, Pururin.DoujinData data)
        {
            Source = client;
            _d = data;
        }

        public string Id => _d.gallery.id.ToString();

        public string PrettyName => _d.gallery.clean_title;
        public string OriginalName => _d.gallery.clean_japan_title;

        // TODO:
        public DateTime UploadTime => DateTime.Now;
        public DateTime ProcessTime => _d._processed;

        public IDoujinClient Source { get; }
        public string SourceUrl => $"https://pururin.io/gallery/{Id}/{_d.gallery.slug}";

        public string Scanlator =>
            _d.gallery.tags.TryGetValue("Scanlator", out var tags) ? tags.FirstOrDefault().slug : null;

        public string Language =>
            _d.gallery.tags.TryGetValue("Language", out var tags) ? tags.FirstOrDefault().slug : null;

        public string ParodyOf =>
            _d.gallery.tags.TryGetValue("Parody", out var tags) ? tags.FirstOrDefault().slug : null;

        public IEnumerable<string> Characters =>
            _d.gallery.tags.TryGetValue("Character", out var tags) ? tags.Select(t => t.slug) : null;

        public IEnumerable<string> Categories => _d.gallery.tags.TryGetValue("Category", out var tags)
            ? tags.Select(t => t.slug).Where(t => t != "doujinshi")
            : null;

        public IEnumerable<string> Artists =>
            _d.gallery.tags.TryGetValue("Artist", out var tags) ? tags.Select(t => t.slug) : null;

        public IEnumerable<string> Tags =>
            _d.gallery.tags.TryGetValue("Contents", out var tags) ? tags.Select(t => t.slug) : null;

        public int PageCount => _d.gallery.total_pages;

        public IEnumerable<PageInfo> Pages
        {
            get
            {
                for (var i = 0; i < _d.gallery.total_pages; i++)
                    yield return new PageInfo(
                        i,
                        "." + _d.gallery.image_extension,
                        Pururin.Image(_d.gallery.id, i, _d.gallery.image_extension));
            }
        }

        public object GetSourceObject() => _d;

        public override string ToString() => PrettyName;
    }
}
