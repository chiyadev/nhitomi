using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents an image in a book content.
    /// </summary>
    [MessagePackObject]
    public class DbBookImage : DbModelBase<BookImage>, IDbModelConvertible<DbBookImage, BookImage>
    {
        [Key("n"), Object(Name = "n", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

        public override void MapTo(BookImage model)
        {
            base.MapTo(model);

            model.Notes = Notes?.ToArray(n => n.Convert()) ?? Array.Empty<ImageNote>();
        }

        public override void MapFrom(BookImage model)
        {
            base.MapFrom(model);

            Notes = model.Notes?.ToArray(p => new DbImageNote().Apply(p));
        }
    }
}