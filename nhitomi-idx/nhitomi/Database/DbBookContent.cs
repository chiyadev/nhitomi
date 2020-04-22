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
        [Key("la"), Keyword(Name = "la")]
        public LanguageType Language { get; set; }

        [Key("pg"), Object(Name = "pg", Enabled = false)]
        public DbBookImage[] Pages { get; set; }

        [Key("sr"), Keyword(Name = "sr")]
        public ScraperType Source { get; set; }

        [Key("da"), Keyword(Name = "da", Index = false)]
        public string Data { get; set; }

        public override void MapTo(BookContent model)
        {
            base.MapTo(model);

            model.Language = Language;
            model.Pages    = Pages?.ToArray(p => p.Convert());
            model.Source   = Source;
        }

        public override void MapFrom(BookContent model)
        {
            base.MapFrom(model);

            Language = model.Language;
            Pages    = model.Pages?.ToArray(p => new DbBookImage().Apply(p));

            // do not map source because Data is valid only for the scraper that initialized it
        }
    }
}