using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using nhitomi.Models;
using nhitomi.Scrapers;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a book's "content".
    /// A book consists of contents that may have a different scraper, language or quality.
    /// </summary>
    [MessagePackObject]
    public class DbBookContent : DbObjectBase<BookContent>, IDbModelConvertible<DbBookContent, BookContent, BookContentBase>
    {
        [Key("pg"), Keyword(Name = "pg", DocValues = false)]
        public int PageCount { get; set; }

        [Key("la"), Keyword(Name = "la", DocValues = false)]
        public LanguageType Language { get; set; }

        [Key("no"), Object(Name = "no", Enabled = false)]
        public Dictionary<int, DbImageNote[]> Notes { get; set; }

        [Key("sr"), Keyword(Name = "sr", DocValues = false)]
        public ScraperType Source { get; set; }

        [Key("si"), Keyword(Name = "si", DocValues = false)]
        public string SourceId { get; set; }

        [Key("Tr"), Date(Name = "Tr")]
        public DateTime? RefreshTime { get; set; }

        [Key("av"), Boolean(Name = "av", DocValues = false)]
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("da"), Keyword(Name = "da", Index = false)]
        public string Data { get; set; }

        public override void MapTo(BookContent model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.PageCount   = PageCount;
            model.Language    = Language;
            model.Notes       = Notes?.ToDictionary(x => x.Key, x => x.Value.ToArray(n => n.Convert(services))) ?? new Dictionary<int, ImageNote[]>();
            model.Source      = Source;
            model.SourceUrl   = services.GetService<IScraperService>().GetBook(Source, out var s) ? s.GetExternalUrl(this) : null;
            model.RefreshTime = RefreshTime;
            model.IsAvailable = IsAvailable;
        }

        public override void MapFrom(BookContent model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            PageCount   = model.PageCount;
            Language    = model.Language;
            Notes       = model.Notes?.ToDictionary(x => x.Key, x => x.Value.ToArray(n => new DbImageNote().Apply(n, services)));
            RefreshTime = model.RefreshTime;
            IsAvailable = model.IsAvailable;

            // do not map Source and SourceId because Data is valid only for the scraper that initialized it
        }
    }
}