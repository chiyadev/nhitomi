using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using nhitomi.Models;

namespace nhitomi.Scrapers
{
    public class nhentaiBook
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("media_id")] public int MediaId;
        [JsonProperty("upload_date")] public long UploadDate;
        [JsonProperty("title")] public TitleData Title;
        [JsonProperty("images")] public ImagesData Images;
        [JsonProperty("tags")] public TagData[] Tags;

        public class TitleData
        {
            [JsonProperty("japanese")] public string Japanese;
            [JsonProperty("english")] public string English;
        }

        public class ImagesData
        {
            [JsonProperty("pages")] public ImageData[] Pages;

            public class ImageData
            {
                [JsonProperty("t")] public char Type;
            }
        }

        public class TagData
        {
            [JsonProperty("type")] public string Type;
            [JsonProperty("name")] public string Name;
        }
    }

    public class nhentaiList
    {
        [JsonProperty("result")] public nhentaiBook[] Results;
        [JsonProperty("num_pages")] public int NumPages;
        [JsonProperty("per_page")] public int PerPage;
    }

    public class nhentaiBookAdaptor : BookAdaptor
    {
        readonly nhentaiBook _book;

        public nhentaiBookAdaptor(nhentaiBook book)
        {
            _book = book;
        }

        // regex to match any () and [] in titles
        static readonly Regex _bracketsRegex = new Regex(@"\([^)]*\)|\[[^\]]*\]|（[^）]*）", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string FixTitle(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            // replace stuff in brackets with nothing
            s = _bracketsRegex.Replace(s, "").Trim();

            // decode html-encoded strings
            s = WebUtility.HtmlDecode(s);

            s = s.Trim();

            return string.IsNullOrEmpty(s) ? null : s;
        }

        // regex to match the convention in title (first parentheses)
        static readonly Regex _conventionRegex = new Regex(@"^\((?<conv>.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string FindConvention(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            return _conventionRegex.Match(s.TrimStart()).Groups["conv"].Value;
        }

        /// <summary>
        /// Some books are incorrectly tagged with a pipe separator.
        /// </summary>
        public static IEnumerable<string> ProcessTag(nhentaiBook.TagData tag) => tag.Name.Split('|', StringSplitOptions.RemoveEmptyEntries);

        public override BookBase Book => new BookBase
        {
            PrimaryName = FixTitle(_book.Title.Japanese) ?? FixTitle(_book.Title.English),
            EnglishName = FixTitle(_book.Title.English) ?? FixTitle(_book.Title.Japanese),

            Tags = new Dictionary<BookTag, string[]>
            {
                // tags CAN be null!
                [BookTag.Artist]     = _book.Tags?.Where(t => t.Type == "artist").ToArrayMany(ProcessTag),
                [BookTag.Tag]        = _book.Tags?.Where(t => t.Type == "tag").ToArrayMany(ProcessTag),
                [BookTag.Parody]     = _book.Tags?.Where(t => t.Type == "parody" && t.Name != "original").ToArrayMany(ProcessTag),
                [BookTag.Character]  = _book.Tags?.Where(t => t.Type == "character").ToArrayMany(ProcessTag),
                [BookTag.Convention] = new[] { FindConvention(_book.Title.English ?? _book.Title.Japanese) },
                [BookTag.Circle]     = _book.Tags?.Where(t => t.Type == "group").ToArrayMany(ProcessTag)
            },

            Category = Enum.TryParse<BookCategory>(_book.Tags?.FirstOrDefault(t => t.Type == "category")?.Name, true, out var category) ? category : BookCategory.Doujinshi,
            Rating   = MaterialRating.Explicit, // explicit by default
        };

        public override IEnumerable<ContentAdaptor> Contents => new[] { new nhentaiContentAdaptor(_book) };
    }

    public class nhentaiContentAdaptor : BookAdaptor.ContentAdaptor
    {
        readonly nhentaiBook _book;

        public nhentaiContentAdaptor(nhentaiBook book)
        {
            _book = book;
        }

        public override string Id => _book.Id.ToString();
        public override int Pages => _book.Images.Pages.Length;

        public override string Data => nhentaiScraper.DataContainer.Serialize(new nhentaiScraper.DataContainer
        {
            MediaId    = _book.MediaId,
            Extensions = string.Concat(_book.Images.Pages.Select(p => p.Type))
        });

        public override BookContentBase Content => new BookContentBase
        {
            Language = _book.Tags?.FirstOrDefault(t => t.Type == "language" && t.Name != "translated")?.Name.ParseAsLanguage() ?? LanguageType.Japanese
        };
    }
}