using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using nhitomi.Models;

namespace nhitomi.Scrapers
{
    public class HitomiBook
    {
        public readonly HitomiGalleryInfo GalleryInfo;

        public HitomiBook(HitomiGalleryInfo galleryInfo)
        {
            GalleryInfo = galleryInfo;
        }

        public string Title;
        public string Artist;
        public string Group;
        public string Series;
        public string[] Characters;
        public string[] Tags;
    }

    public class HitomiGalleryIdentity
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("type")] public string Type;
        [JsonProperty("title")] public string Title;
        [JsonProperty("language_localname")] public string LanguageLocalName;
    }

    public class HitomiGalleryInfo : HitomiGalleryIdentity
    {
        [JsonProperty("language")] public string Language;
        [JsonProperty("date")] public DateTime Date;
        [JsonProperty("files")] public FileData[] Files;
        [JsonProperty("tags")] public TagData[] Tags;

        public class FileData
        {
            [JsonProperty("width")] public int Width;
            [JsonProperty("height")] public int Height;
            [JsonProperty("hash")] public string Hash;
            [JsonProperty("haswebp")] public bool HasWebP;
            [JsonProperty("hasavif")] public bool HasAvif;
            [JsonProperty("name")] public string Name;
        }

        public class TagData
        {
            [JsonProperty("url")] public string Url;
            [JsonProperty("tag")] public string Tag;
        }
    }

    public class HitomiBookAdaptor : BookAdaptor
    {
        readonly HitomiBook _book;

        public HitomiBookAdaptor(HitomiBook book)
        {
            _book = book;

            ParseName(_book.Title, out _primaryName, out _englishName);
        }

        // regex to match any () and [] in titles
        static readonly Regex _bracketsRegex = new Regex(@"\([^)]*\)|\[[^\]]*\]|（[^）]*）", RegexOptions.Compiled | RegexOptions.Singleline);

        static void ParseName(string name, out string primaryName, out string englishName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                primaryName = null;
                englishName = null;
                return;
            }

            // replace stuff in brackets with nothing
            name = _bracketsRegex.Replace(name, "").Trim();

            // hitomi books are not consistently named
            // they can be fully localized, romanized, english translated, or any combination of these separated by a pipe with no specific order
            // we try our best to accurately determine which is localized/romanized and which is english by checking each word against an English word corpus
            var pipe = name.IndexOf('|');

            if (pipe == -1)
            {
                primaryName = name;
                englishName = name;
                return;
            }

            var a = name.Substring(0, pipe).Trim();
            var b = name.Substring(pipe + 1).Trim(); // more english-like

            if (a.Length == 0) a = b;
            if (b.Length == 0) b = a;

            if (a != b)
            {
                static int getScore(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(English.IsEnglish);

                if (getScore(a) > getScore(b))
                    (a, b) = (b, a);
            }

            primaryName = a;
            englishName = b;
        }

        readonly string _primaryName;
        readonly string _englishName;

        static string ConvertTag(string tag) => tag?.TrimEnd('♀', '♂', ' ').ToLowerInvariant();

        static string ConvertSeries(string series)
        {
            var tag = ConvertTag(series);

            return tag == "original" ? null : tag;
        }

        public override BookBase Book => new BookBase
        {
            PrimaryName = _primaryName,
            EnglishName = _englishName,

            Tags = new Dictionary<BookTag, string[]>
            {
                [BookTag.Artist]    = new[] { ConvertTag(_book.Artist) },
                [BookTag.Circle]    = new[] { ConvertTag(_book.Group) },
                [BookTag.Parody]    = new[] { ConvertSeries(_book.Series) },
                [BookTag.Character] = _book.Characters?.ToArray(ConvertTag),
                [BookTag.Tag]       = _book.Tags?.ToArray(ConvertTag)
            },

            Category = Enum.TryParse<BookCategory>(_book.GalleryInfo.Type, out var category) ? category : BookCategory.Doujinshi,
            Rating   = MaterialRating.Explicit // explicit by default
        };

        public override IEnumerable<ContentAdaptor> Contents => new[] { new HitomiContentAdaptor(_book) };
    }

    public class HitomiContentAdaptor : BookAdaptor.ContentAdaptor
    {
        readonly HitomiBook _book;

        public HitomiContentAdaptor(HitomiBook book)
        {
            _book = book;
        }

        public override string Id => _book.GalleryInfo.Id.ToString();

        public override string Data => HitomiScraper.DataContainer.Serialize(new HitomiScraper.DataContainer
        {
            // gallery identity
            Id                = _book.GalleryInfo.Id,
            Type              = _book.GalleryInfo.Type,
            Title             = _book.GalleryInfo.Title,
            LanguageLocalName = _book.GalleryInfo.LanguageLocalName,

            Hashes = _book.GalleryInfo.Files.ToArray(f => HitomiScraper.DataContainer.CompressHash(f.Hash) + Path.GetExtension(f.Name))
        });

        public override int Pages => _book.GalleryInfo.Files.Length;

        public override BookContentBase Content => new BookContentBase
        {
            Language = _book.GalleryInfo.Language?.ParseAsLanguage() ?? LanguageType.Japanese
        };
    }
}