using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a book.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Book))]
    public class DbBook : DbObjectBase<Book>, IDbModelConvertible<DbBook, Book, BookBase>, IHasUpdatedTime, IDbSupportsAutocomplete
    {
        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("np"), Text(Name = "np")]
        public string PrimaryName { get; set; }

        [Key("ne"), Text(Name = "ne")]
        public string EnglishName { get; set; }

        [Key("tg"), Text(Name = "tg")]
        public string[] TagsGeneral { get; set; }

        [Key("ta"), Text(Name = "ta")]
        public string[] TagsArtist { get; set; }

        [Key("tp"), Text(Name = "tp")]
        public string[] TagsParody { get; set; }

        [Key("tc"), Text(Name = "tc")]
        public string[] TagsCharacter { get; set; }

        [Key("tco"), Text(Name = "tco")]
        public string[] TagsConvention { get; set; }

        [Key("ts"), Text(Name = "ts")]
        public string[] TagsSeries { get; set; }

        [Key("tci"), Text(Name = "tci")]
        public string[] TagsCircle { get; set; }

        [Key("tm"), Text(Name = "tm")]
        public string[] TagsMetadata { get; set; }

        [Key("ca"), Keyword(Name = "ca")]
        public BookCategory Category { get; set; }

        [Key("la"), Keyword(Name = "la")]
        public LanguageType Language { get; set; }

        [Key("ra"), Keyword(Name = "ra")]
        public MaterialRating Rating { get; set; }

        [Key("sr"), Keyword(Name = "sr")]
        public string[] Sources { get; set; }

        [Key("pg"), Object(Name = "pg", Enabled = false)]
        public DbBookImage[] Pages { get; set; }

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
            model.Language = Language;
            model.Rating   = Rating;
            model.Sources  = Sources?.ToArray(WebsiteSource.Parse);

            model.Pages = Pages?.ToArray(p => p.Convert());
        }

        public override void MapFrom(Book model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            PrimaryName = model.PrimaryName;
            EnglishName = model.EnglishName;

            if (model.Tags != null)
            {
                TagsGeneral    = model.Tags.GetValueOrDefault(BookTag.Tag);
                TagsArtist     = model.Tags.GetValueOrDefault(BookTag.Artist);
                TagsParody     = model.Tags.GetValueOrDefault(BookTag.Parody);
                TagsCharacter  = model.Tags.GetValueOrDefault(BookTag.Character);
                TagsConvention = model.Tags.GetValueOrDefault(BookTag.Convention);
                TagsSeries     = model.Tags.GetValueOrDefault(BookTag.Series);
                TagsCircle     = model.Tags.GetValueOrDefault(BookTag.Circle);
                TagsMetadata   = model.Tags.GetValueOrDefault(BookTag.Metadata);
            }

            Category = model.Category;
            Language = model.Language;
            Rating   = model.Rating;
            Sources  = model.Sources?.ToArray(s => s.ToString());

            Pages = model.Pages?.ToArray(p => new DbBookImage().Apply(p));
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "pc")]
        public int PageCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "nc")]
        public int NoteCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "tgc")]
        public int TagCount { get; set; }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "sc")]
        public int SourceCount { get; set; }

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

            PageCount = Pages?.Length ?? 0;
            NoteCount = Pages?.Sum(p => p.Notes?.Length ?? 0) ?? 0;

            TagCount = new[]
                {
                    TagsGeneral,
                    TagsArtist,
                    TagsParody,
                    TagsCharacter,
                    TagsConvention,
                    TagsSeries,
                    TagsCircle,
                    TagsMetadata
                }.SelectMany(x => x)
                 .Count();

            SourceCount = Sources?.Length ?? 0;

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