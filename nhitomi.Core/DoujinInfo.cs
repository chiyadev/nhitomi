using System;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Core
{
    public class DoujinInfo
    {
        public string GalleryUrl { get; set; }

        public string PrettyName { get; set; }
        public string OriginalName { get; set; }

        public DateTime UploadTime { get; set; }

        public IDoujinClient Source { get; set; }
        public string SourceId { get; set; }

        public string Scanlator { get; set; }
        public string Language { get; set; }
        public string ParodyOf { get; set; }

        public IEnumerable<string> Characters { get; set; }
        public IEnumerable<string> Categories { get; set; }
        public IEnumerable<string> Artists { get; set; }
        public IEnumerable<string> Groups { get; set; }
        public IEnumerable<string> Tags { get; set; }

        public IEnumerable<string> Images { get; set; }

        public Doujin ToDoujin() => new Doujin
        {
            GalleryUrl = GalleryUrl,
            PrettyName = PrettyName,
            OriginalName = OriginalName,
            UploadTime = UploadTime,
            Source = Source.Name,
            SourceId = SourceId,
            Scanlator = IsNull(Scanlator) ? null : new Scanlator {Value = Scanlator},
            Language = IsNull(Language) ? null : new Language {Value = Language},
            ParodyOf = IsNull(ParodyOf) ? null : new ParodyOf {Value = ParodyOf},
            Characters = Characters?.Where(IsSpecified).Select(c => new Character {Value = c}).ToList(),
            Categories = Categories?.Where(IsSpecified).Select(c => new Category {Value = c}).ToList(),
            Artists = Artists?.Where(IsSpecified).Select(a => new Artist {Value = a}).ToList(),
            Groups = Artists?.Where(IsSpecified).Select(g => new Group {Value = g}).ToList(),
            Tags = Artists?.Where(IsSpecified).Select(t => new Tag {Value = t}).ToList(),
            Pages = Images?.Where(IsSpecified).Select(p => new Page {Url = p}).ToList()
        };

        static bool IsNull(string str) => string.IsNullOrEmpty(str);
        static bool IsSpecified(string str) => !IsNull(str);
    }
}