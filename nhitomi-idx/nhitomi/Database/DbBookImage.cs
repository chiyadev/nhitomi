using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents an image in a book content.
    /// Position of an image is determined by its index in <see cref="BookContent.Pages"/>.
    /// </summary>
    [MessagePackObject]
    public class DbBookImage : DbModelBase<BookImage>, IDbModelConvertible<DbBookImage, BookImage>, IDbSupportsPieces
    {
        [Key("w"), Number(Name = "w")]
        public int Width { get; set; }

        [Key("h"), Number(Name = "h")]
        public int Height { get; set; }

        [Key("p"), Object(Name = "p", Enabled = false)]
        public DbPiece[] Pieces { get; set; }

        public override void MapTo(BookImage model)
        {
            base.MapTo(model);

            model.Width  = Width;
            model.Height = Height;

            model.Pieces = Pieces?.ToArray(p => p.Convert());
        }

        public override void MapFrom(BookImage model)
        {
            base.MapFrom(model);

            Width  = model.Width;
            Height = model.Height;

            Pieces = model.Pieces?.ToArray(p => new DbPiece().Apply(p));
        }
    }
}