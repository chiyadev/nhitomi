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

        [Key("tg"), Keyword(Name = "tg", DocValues = false)]
        public string[] TagsGeneral { get; set; }

        [Key("ta"), Keyword(Name = "ta", DocValues = false)]
        public string[] TagsArtist { get; set; }

        [Key("tp"), Keyword(Name = "tp", DocValues = false)]
        public string[] TagsParody { get; set; }

        [Key("tc"), Keyword(Name = "tc", DocValues = false)]
        public string[] TagsCharacter { get; set; }

        [Key("tco"), Keyword(Name = "tco", DocValues = false)]
        public string[] TagsConvention { get; set; }

        [Key("ts"), Keyword(Name = "ts", DocValues = false)]
        public string[] TagsSeries { get; set; }

        [Key("tci"), Keyword(Name = "tci", DocValues = false)]
        public string[] TagsCircle { get; set; }

        [Key("tm"), Keyword(Name = "tm", DocValues = false)]
        public string[] TagsMetadata { get; set; }

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

            model.Tags = new Dictionary<BookTag, string[]>
            {
                [BookTag.Tag]        = TagsGeneral,
                [BookTag.Artist]     = TagsArtist,
                [BookTag.Parody]     = TagsParody,
                [BookTag.Character]  = TagsCharacter,
                [BookTag.Convention] = TagsConvention,
                [BookTag.Series]     = TagsSeries,
                [BookTag.Circle]     = TagsCircle,
                [BookTag.Metadata]   = TagsMetadata
            };

            model.Category = Category;
            model.Rating   = Rating;

            model.Contents = Contents?.ToArray(c => c.Convert());
        }

        public override void MapFrom(Book model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            PrimaryName = model.PrimaryName;
            EnglishName = model.EnglishName;

            TagsGeneral    = model.Tags?.GetValueOrDefault(BookTag.Tag);
            TagsArtist     = model.Tags?.GetValueOrDefault(BookTag.Artist);
            TagsParody     = model.Tags?.GetValueOrDefault(BookTag.Parody);
            TagsCharacter  = model.Tags?.GetValueOrDefault(BookTag.Character);
            TagsConvention = model.Tags?.GetValueOrDefault(BookTag.Convention);
            TagsSeries     = model.Tags?.GetValueOrDefault(BookTag.Series);
            TagsCircle     = model.Tags?.GetValueOrDefault(BookTag.Circle);
            TagsMetadata   = model.Tags?.GetValueOrDefault(BookTag.Metadata);

            Category = model.Category;
            Rating   = model.Rating;

            Contents = model.Contents?.ToArray(c => new DbBookContent().Apply(c));
        }

        public void MergeFrom(DbBook other)
        {
            TagsGeneral    = TagsGeneral.DistinctMergeSafe(other.TagsGeneral);
            TagsArtist     = TagsArtist.DistinctMergeSafe(other.TagsArtist);
            TagsParody     = TagsParody.DistinctMergeSafe(other.TagsParody);
            TagsCharacter  = TagsCharacter.DistinctMergeSafe(other.TagsCharacter);
            TagsConvention = TagsConvention.DistinctMergeSafe(other.TagsConvention);
            TagsSeries     = TagsSeries.DistinctMergeSafe(other.TagsSeries);
            TagsCircle     = TagsCircle.DistinctMergeSafe(other.TagsCircle);
            TagsMetadata   = TagsMetadata.DistinctMergeSafe(other.TagsMetadata);

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

        public enum SuggestionType
        {
            PrimaryName = -1,
            EnglishName = -2,
            Tags = BookTag.Tag,
            TagsArtist = BookTag.Artist,
            TagsParody = BookTag.Parody,
            TagsCharacter = BookTag.Character,
            TagsConvention = BookTag.Convention,
            TagsSeries = BookTag.Series,
            TagsCircle = BookTag.Circle,
            TagsMetadata = BookTag.Metadata
        }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = IDbSupportsAutocomplete.SuggestField, PreserveSeparators = false, PreservePositionIncrements = false)]
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

            TagCount = new[]
                {
                    TagsGeneral,
                    TagsArtist,
                    TagsParody,
                    TagsCharacter,
                    TagsConvention,
                    TagsSeries,
                    TagsCircle,
                    TagsMetadata,
                }.SelectMany(x => x)
                 .Count();

            Language = Contents?.ToArray(c => c.Language);
            Sources  = Contents?.ToArray(c => c.Source);

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(
                    (SuggestionType.PrimaryName, new[] { PrimaryName }),
                    (SuggestionType.EnglishName, new[] { EnglishName }),
                    (SuggestionType.Tags, TagsGeneral),
                    (SuggestionType.TagsArtist, TagsArtist),
                    (SuggestionType.TagsParody, TagsParody),
                    (SuggestionType.TagsCharacter, TagsCharacter),
                    (SuggestionType.TagsConvention, TagsConvention),
                    (SuggestionType.TagsSeries, TagsSeries),
                    (SuggestionType.TagsCircle, TagsCircle),
                    (SuggestionType.TagsMetadata, TagsMetadata))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbBook book) => new nhitomiObject(ObjectType.Book, book.Id);
    }
}