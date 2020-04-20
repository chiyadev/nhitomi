using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a book's "content".
    /// A book consists of contents that may be available in different languages or quality.
    /// </summary>
    [MessagePackObject]
    public class DbBookContent : DbObjectBase<BookContent>, IDbModelConvertible<DbBookContent, BookContent, BookContentBase>, IDbSupportsAvailability
    {
        [Key("la"), Keyword(Name = "la")]
        public LanguageType Language { get; set; }

        [Key("sr"), Keyword(Name = "sr")]
        public string[] Sources { get; set; }

        [Key("pg"), Object(Name = "pg", Enabled = false)]
        public DbBookImage[] Pages { get; set; }

        [Key("a"), Number(Name = "aa")]
        public double Availability { get; set; }

        [Key("A"), Number(Name = "at")]
        public double TotalAvailability { get; set; }

        public override void MapTo(BookContent model)
        {
            base.MapTo(model);

            model.Language = Language;
            model.Sources  = Sources?.ToArray(WebsiteSource.Parse);

            model.Pages = Pages?.ToArray(p => p.Convert());

            model.Availability      = Availability;
            model.TotalAvailability = TotalAvailability;
        }

        public override void MapFrom(BookContent model)
        {
            base.MapFrom(model);

            Language = model.Language;
            Sources  = model.Sources?.ToArray(s => s.ToString());

            Pages = model.Pages?.ToArray(p => new DbBookImage().Apply(p));

            Availability      = model.Availability;
            TotalAvailability = model.TotalAvailability;
        }
    }
}