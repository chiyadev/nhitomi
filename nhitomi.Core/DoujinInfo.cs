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

        public string Artist { get; set; }
        public string Group { get; set; }
        public string Scanlator { get; set; }
        public string Language { get; set; }
        public string Parody { get; set; }

        public IEnumerable<string> Characters { get; set; }
        public IEnumerable<string> Categories { get; set; }
        public IEnumerable<string> Tags { get; set; }

        public IEnumerable<string> Images { get; set; }

        public Doujin ToDoujin() => new Doujin
        {
            GalleryUrl = GalleryUrl,

            PrettyName = PrettyName,
            OriginalName = OriginalName,

            UploadTime = UploadTime,
            ProcessTime = DateTime.UtcNow,

            Source = Source.Name,
            SourceId = SourceId,

            Tags = CreateTagRefs().ToList()
        };

        IEnumerable<TagRef> CreateTagRefs()
        {
            yield return new TagRef(TagType.Artist, Artist);
            yield return new TagRef(TagType.Group, Group);
            yield return new TagRef(TagType.Scanlator, Scanlator);
            yield return new TagRef(TagType.Language, Language);
            yield return new TagRef(TagType.Parody, Parody);

            foreach (var character in Characters)
                yield return new TagRef(TagType.Character, character);

            foreach (var category in Categories)
                yield return new TagRef(TagType.Category, category);

            foreach (var tag in Tags)
                yield return new TagRef(TagType.Tag, tag);
        }

        static bool IsNull(string str) => string.IsNullOrEmpty(str);
        static bool IsSpecified(string str) => !IsNull(str);
    }
}