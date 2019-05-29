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

        public string Data { get; set; }
        public int PageCount { get; set; }

        public Doujin ToDoujin() => new Doujin
        {
            GalleryUrl = GalleryUrl,

            PrettyName = PrettyName,
            OriginalName = OriginalName,

            UploadTime = UploadTime,
            ProcessTime = DateTime.UtcNow,

            Source = Source.Name,
            SourceId = SourceId,

            Data = Data,
            PageCount = PageCount,

            Tags = CreateTagRefs().ToList()
        };

        IEnumerable<TagRef> CreateTagRefs()
        {
            if (!string.IsNullOrWhiteSpace(Artist))
                yield return new TagRef(TagType.Artist, Artist);

            if (!string.IsNullOrWhiteSpace(Group))
                yield return new TagRef(TagType.Group, Group);

            if (!string.IsNullOrWhiteSpace(Scanlator))
                yield return new TagRef(TagType.Scanlator, Scanlator);

            if (!string.IsNullOrWhiteSpace(Language))
                yield return new TagRef(TagType.Language, Language);

            if (!string.IsNullOrWhiteSpace(Parody))
                yield return new TagRef(TagType.Parody, Parody);

            if (Characters != null)
                foreach (var character in Characters)
                    yield return new TagRef(TagType.Character, character);

            if (Categories != null)
                foreach (var category in Categories)
                    yield return new TagRef(TagType.Category, category);

            if (Tags != null)
                foreach (var tag in Tags)
                    yield return new TagRef(TagType.Tag, tag);
        }
    }
}