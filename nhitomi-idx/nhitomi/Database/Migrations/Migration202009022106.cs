using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChiyaFlake;
using Microsoft.Extensions.Logging;
using Nest;
using nhitomi.Models;
using nhitomi.Scrapers;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Adds availability field to books.
    /// </summary>
    public class Migration202009022106 : MigrationBase
    {
        public Migration202009022106(IServiceProvider services, ILogger<Migration202009022106> logger) : base(services, logger) { }

        public class DbBook : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Date(Name = "Tc")]
            public DateTime CreatedTime { get; set; }

            [Date(Name = "Tu")]
            public DateTime UpdatedTime { get; set; }

            [Text(Name = "np")]
            public string PrimaryName { get; set; }

            [Text(Name = "ne")]
            public string EnglishName { get; set; }

            [Text(Name = "tg")]
            public string[] TagsGeneral { get; set; }

            [Text(Name = "ta")]
            public string[] TagsArtist { get; set; }

            [Text(Name = "tp")]
            public string[] TagsParody { get; set; }

            [Text(Name = "tc")]
            public string[] TagsCharacter { get; set; }

            [Text(Name = "tco")]
            public string[] TagsConvention { get; set; }

            [Text(Name = "ts")]
            public string[] TagsSeries { get; set; }

            [Text(Name = "tci")]
            public string[] TagsCircle { get; set; }

            [Text(Name = "tm")]
            public string[] TagsMetadata { get; set; }

            [Keyword(Name = "ca", DocValues = false)]
            public BookCategory Category { get; set; }

            [Keyword(Name = "ra", DocValues = false)]
            public MaterialRating Rating { get; set; }

            [Object(Name = "co", Enabled = false)]
            public DbBookContent[] Contents { get; set; }

#region Cached

            [Number(Name = "pc"), DbCached]
            public int[] PageCount { get; set; }

            [Number(Name = "nc"), DbCached]
            public int[] NoteCount { get; set; }

            [Number(Name = "tC"), DbCached]
            public int TagCount { get; set; }

            [Keyword(Name = "ln", DocValues = false), DbCached]
            public LanguageType[] Language { get; set; }

            [Keyword(Name = "sr", DocValues = false), DbCached]
            public ScraperType[] Sources { get; set; }

            [Keyword(Name = "si", DocValues = false), DbCached]
            public string[] SourceIds { get; set; }

            [Date(Name = "Tr"), DbCached]
            public DateTime?[] RefreshTime { get; set; }

            [Boolean(Name = "av", DocValues = false), DbCached]
            public bool[] IsAvailable { get; set; }

            [Completion(Name = "sug", PreserveSeparators = false, PreservePositionIncrements = false), DbCached]
            public CompletionField Suggest { get; set; }

            public void UpdateCache(IServiceProvider services)
            {
                // auto-set content ids
                if (Contents != null)
                    foreach (var content in Contents)
                        content.Id ??= Snowflake.New;

                var tags = new Dictionary<BookTag, string[]>
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

                PageCount   = Contents?.ToArray(c => c.PageCount);
                NoteCount   = Contents?.ToArray(c => c.Notes?.Values.Sum(x => x?.Length ?? 0) ?? 0);
                TagCount    = tags.Values.Sum(x => x?.Length ?? 0);
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
                        foreach (var (key, value) in tags)
                            d[(int) key] = value;
                    }))
                };
            }

#endregion
        }

        public class DbBookContent
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Keyword(Name = "pg", DocValues = false)]
            public int PageCount { get; set; }

            [Keyword(Name = "la", DocValues = false)]
            public LanguageType Language { get; set; }

            [Object(Name = "no", Enabled = false)]
            public Dictionary<int, Migration202001010000.DbImageNote[]> Notes { get; set; }

            [Keyword(Name = "sr", DocValues = false)]
            public ScraperType Source { get; set; }

            [Keyword(Name = "si", DocValues = false)]
            public string SourceId { get; set; }

            [Date(Name = "Tr")]
            public DateTime? RefreshTime { get; set; }

            [Boolean(Name = "av", DocValues = false)]
            public bool IsAvailable { get; set; }

            [Keyword(Name = "da", Index = false)]
            public string Data { get; set; }
        }

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var (source, destination) = await GetReindexTargets("book", cancellationToken);

            await MapIndexAsync<Migration202001010000.DbBook, DbBook>(source, destination, b => new DbBook
            {
                Id             = b.Id,
                CreatedTime    = b.CreatedTime,
                UpdatedTime    = b.UpdatedTime,
                PrimaryName    = b.PrimaryName,
                EnglishName    = b.EnglishName,
                TagsGeneral    = b.TagsGeneral,
                TagsArtist     = b.TagsArtist,
                TagsParody     = b.TagsParody,
                TagsCharacter  = b.TagsCharacter,
                TagsConvention = b.TagsConvention,
                TagsSeries     = b.TagsSeries,
                TagsCircle     = b.TagsCircle,
                TagsMetadata   = b.TagsMetadata,
                Category       = b.Category,
                Rating         = b.Rating,

                Contents = b.Contents?.ToArray(c => new DbBookContent
                {
                    Id          = c.Id,
                    PageCount   = c.PageCount,
                    Language    = c.Language,
                    Notes       = c.Notes,
                    Source      = c.Source,
                    SourceId    = c.SourceId,
                    RefreshTime = null,
                    IsAvailable = true, // available by default
                    Data        = c.Data,
                }),
            }, cancellationToken);
        }
    }
}