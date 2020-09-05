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
    public class DbBook : DbObjectBase<Book>, IDbHasType, IDbModelConvertible<DbBook, Book, BookBase>, IHasUpdatedTime, IDbSupportsAutocomplete
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

        [Key("tg"), Text(Name = "tg")]
        public string[] TagsGeneral
        {
            get => GetTags(BookTag.Tag);
            set => SetTags(BookTag.Tag, value);
        }

        [Key("ta"), Text(Name = "ta")]
        public string[] TagsArtist
        {
            get => GetTags(BookTag.Artist);
            set => SetTags(BookTag.Artist, value);
        }

        [Key("tp"), Text(Name = "tp")]
        public string[] TagsParody
        {
            get => GetTags(BookTag.Parody);
            set => SetTags(BookTag.Parody, value);
        }

        [Key("tc"), Text(Name = "tc")]
        public string[] TagsCharacter
        {
            get => GetTags(BookTag.Character);
            set => SetTags(BookTag.Character, value);
        }

        [Key("tco"), Text(Name = "tco")]
        public string[] TagsConvention
        {
            get => GetTags(BookTag.Convention);
            set => SetTags(BookTag.Convention, value);
        }

        [Key("ts"), Text(Name = "ts")]
        public string[] TagsSeries
        {
            get => GetTags(BookTag.Series);
            set => SetTags(BookTag.Series, value);
        }

        [Key("tci"), Text(Name = "tci")]
        public string[] TagsCircle
        {
            get => GetTags(BookTag.Circle);
            set => SetTags(BookTag.Circle, value);
        }

        [Key("tm"), Text(Name = "tm")]
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

        public override void MapTo(Book model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.CreatedTime = CreatedTime;
            model.UpdatedTime = UpdatedTime;
            model.PrimaryName = PrimaryName;
            model.EnglishName = EnglishName;
            model.Tags        = _tags.DictClone();
            model.Category    = Category;
            model.Rating      = Rating;
            model.Contents    = Contents?.ToArray(c => c.Convert(services));

            // ensure all tags are displayed sorted
            foreach (var tags in model.Tags.Values)
            {
                if (tags != null)
                    Array.Sort(tags);
            }

            if (model.Contents != null)
                Array.Sort(model.Contents, (a, b) =>
                {
                    // sort by source first
                    var source = a.Source.CompareTo(b.Source);

                    if (source != 0)
                        return source;

                    // then by id descending (i.e. latest contents first)
                    var id = -string.Compare(a.Id, b.Id, StringComparison.Ordinal);

                    return id;
                });
        }

        public override void MapFrom(Book model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            PrimaryName = model.PrimaryName;
            EnglishName = model.EnglishName;
            _tags       = model.Tags?.DictClone() ?? new Dictionary<BookTag, string[]>();
            Category    = model.Category;
            Rating      = model.Rating;
            Contents    = model.Contents?.ToArray(c => new DbBookContent().Apply(c, services));
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
        [IgnoreMember, Number(Name = "pc"), DbCached]
        public int[] PageCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "nc"), DbCached]
        public int[] NoteCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "tC"), DbCached]
        public int TagCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "ln", DocValues = false), DbCached]
        public LanguageType[] Language { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "sr", DocValues = false), DbCached]
        public ScraperType[] Sources { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Keyword(Name = "si", DocValues = false), DbCached]
        public string[] SourceIds { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Date(Name = "Tr"), DbCached]
        public DateTime?[] RefreshTime { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Boolean(Name = "av", DocValues = false), DbCached]
        public bool[] IsAvailable { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = "sug", PreserveSeparators = false, PreservePositionIncrements = false), DbCached]
        public CompletionField Suggest { get; set; }

        public override void UpdateCache(IServiceProvider services)
        {
            base.UpdateCache(services);

            // auto-set content ids
            if (Contents != null)
                foreach (var content in Contents)
                    content.Id ??= Snowflake.New;

            PageCount   = Contents?.ToArray(c => c.PageCount);
            NoteCount   = Contents?.ToArray(c => c.Notes?.Values.Sum(x => x?.Length ?? 0) ?? 0);
            TagCount    = _tags.Values.Sum(x => x?.Length ?? 0);
            Language    = Contents?.ToArray(c => c.Language);
            Sources     = Contents?.ToArray(c => c.Source);
            SourceIds   = Contents?.ToArray(c => c.SourceId);
            RefreshTime = Contents?.ToArray(c => c.RefreshTime);
            IsAvailable = Contents?.ToArray(c => c.IsAvailable);

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(new Dictionary<int, string[]>
                {
                    [-1] = new[] { PrimaryName },
                    [-2] = new[] { EnglishName }
                }.Chain(d =>
                {
                    foreach (var (key, value) in _tags)
                        d[(int) key] = value;
                }))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbBook book) => new nhitomiObject(ObjectType.Book, book.Id);
    }
}