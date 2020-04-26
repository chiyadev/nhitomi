using System;
using System.Collections.Generic;
using System.Linq;
using ChiyaFlake;
using MessagePack;
using Nest;
using nhitomi.Models;
using nhitomi.Scrapers;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a book.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Book))]
    public class DbBook : DbObjectBase<Book>, IDbHasType, IDbModelConvertible<DbBook, Book, BookBase>, IHasUpdatedTime, IDbSupportsAutocomplete, ISanitizableObject
    {
        [IgnoreMember, Ignore]
        ObjectType IDbHasType.Type => ObjectType.Book;

        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("np"), Text(Name = "np")]
        public string PrimaryName { get; set; }

        [Key("ne"), Text(Name = "ne")]
        public string EnglishName { get; set; }

        Dictionary<BookTag, string[]> _tags = new Dictionary<BookTag, string[]>();

        public string[] GetTags(BookTag tag) => _tags.GetValueOrDefault(tag);
        public void SetTags(BookTag tag, string[] value) => _tags[tag] = value;

        [Key("tg"), Keyword(Name = "tg", DocValues = false)]
        public string[] TagsGeneral
        {
            get => GetTags(BookTag.Tag);
            set => SetTags(BookTag.Tag, value);
        }

        [Key("ta"), Keyword(Name = "ta", DocValues = false)]
        public string[] TagsArtist
        {
            get => GetTags(BookTag.Artist);
            set => SetTags(BookTag.Artist, value);
        }

        [Key("tp"), Keyword(Name = "tp", DocValues = false)]
        public string[] TagsParody
        {
            get => GetTags(BookTag.Parody);
            set => SetTags(BookTag.Parody, value);
        }

        [Key("tc"), Keyword(Name = "tc", DocValues = false)]
        public string[] TagsCharacter
        {
            get => GetTags(BookTag.Character);
            set => SetTags(BookTag.Character, value);
        }

        [Key("tco"), Keyword(Name = "tco", DocValues = false)]
        public string[] TagsConvention
        {
            get => GetTags(BookTag.Convention);
            set => SetTags(BookTag.Convention, value);
        }

        [Key("ts"), Keyword(Name = "ts", DocValues = false)]
        public string[] TagsSeries
        {
            get => GetTags(BookTag.Series);
            set => SetTags(BookTag.Series, value);
        }

        [Key("tci"), Keyword(Name = "tci", DocValues = false)]
        public string[] TagsCircle
        {
            get => GetTags(BookTag.Circle);
            set => SetTags(BookTag.Circle, value);
        }

        [Key("tm"), Keyword(Name = "tm", DocValues = false)]
        public string[] TagsMetadata
        {
            get => GetTags(BookTag.Metadata);
            set => SetTags(BookTag.Metadata, value);
        }

        [Key("ca"), Keyword(Name = "ca", DocValues = false)]
        public BookCategory Category { get; set; }

        [Key("ra"), Keyword(Name = "ra", DocValues = false)]
        public MaterialRating Rating { get; set; }

        [Key("co"), Object(Name = "co", Enabled = false)]
        public DbBookContent[] Contents { get; set; }

        public override void MapTo(Book model)
        {
            base.MapTo(model);

            model.CreatedTime = CreatedTime;
            model.UpdatedTime = UpdatedTime;
            model.PrimaryName = PrimaryName;
            model.EnglishName = EnglishName;
            model.Tags        = _tags.DictClone();
            model.Category    = Category;
            model.Rating      = Rating;
            model.Contents    = Contents?.ToArray(c => c.Convert());
        }

        public override void MapFrom(Book model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            PrimaryName = model.PrimaryName;
            EnglishName = model.EnglishName;
            _tags       = model.Tags?.DictClone() ?? new Dictionary<BookTag, string[]>();
            Category    = model.Category;
            Rating      = model.Rating;
            Contents    = model.Contents?.ToArray(c => new DbBookContent().Apply(c));
        }

        public void MergeFrom(DbBook other)
        {
            _tags    = _tags.DistinctMergeSafe(other._tags);
            Contents = Contents.DistinctMergeSafe(other.Contents);
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "pc")]
        public int[] PageCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "nc")]
        public int[] NoteCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "tC")]
        public int TagCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "ln", DocValues = false)]
        public LanguageType[] Language { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "sr", DocValues = false)]
        public ScraperType[] Sources { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "si", DocValues = false)]
        public string[] SourceIds { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = IDbSupportsAutocomplete.SuggestField, PreserveSeparators = false, PreservePositionIncrements = false), SanitizerIgnore]
        public CompletionField Suggest { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            // auto-set content ids
            if (Contents != null)
                foreach (var content in Contents)
                    content.Id ??= Snowflake.New;

            PageCount = Contents?.ToArray(c => c.Pages?.Length ?? 0);
            NoteCount = Contents?.ToArray(c => c.Pages?.Sum(p => p.Notes?.Length ?? 0) ?? 0);
            TagCount  = _tags.Values.Sum(x => x?.Length ?? 0);
            Language  = Contents?.ToArray(c => c.Language);
            Sources   = Contents?.ToArray(c => c.Source);
            SourceIds = Contents?.ToArray(c => c.SourceId);

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(new Dictionary<int, string[]>
                {
                    [-1] = new[] { PrimaryName },
                    [-2] = new[] { EnglishName }
                }.Compose(d =>
                {
                    foreach (var (key, value) in _tags)
                        d[(int) key] = value;

                    return d;
                }))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbBook book) => new nhitomiObject(ObjectType.Book, book.Id);

        public void BeforeSanitize()
        {
            foreach (var (key, value) in _tags.DictClone())
                _tags[key] = TagFormatter.Format(value);
        }

        void ISanitizableObject.AfterSanitize() { }
    }
}