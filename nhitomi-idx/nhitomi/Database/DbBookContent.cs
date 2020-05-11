using System;
using MessagePack;
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
        [Key("la"), Keyword(Name = "la", DocValues = false)]
        public LanguageType Language { get; set; }

        [Key("pg"), Object(Name = "pg", Enabled = false)]
        public DbBookImage[] Pages { get; set; }

        [Key("sr"), Keyword(Name = "sr", DocValues = false)]
        public ScraperType Source { get; set; }

        [Key("si"), Keyword(Name = "si", DocValues = false)]
        public string SourceId { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("da"), Keyword(Name = "da", Index = false)]
        public string Data { get; set; }

        public override void MapTo(BookContent model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Language = Language;
            model.Pages    = Pages?.ToArray(p => p.Convert(services));
            model.Source   = Source;
        }

        public override void MapFrom(BookContent model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Language = model.Language;
            Pages    = model.Pages?.ToArray(p => new DbBookImage().Apply(p, services));

            // do not map source because Data is valid only for the scraper that initialized it
        }
    }
}