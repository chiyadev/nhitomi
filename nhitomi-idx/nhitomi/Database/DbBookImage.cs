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

        public override void MapTo(BookImage model, IServiceProvider services)
        {
            base.MapTo(model, services);

            model.Notes = Notes?.ToArray(n => n.Convert(services)) ?? Array.Empty<ImageNote>();
        }

        public override void MapFrom(BookImage model, IServiceProvider services)
        {
            base.MapFrom(model, services);

            Notes = model.Notes?.ToArray(p => new DbImageNote().Apply(p, services));
        }
    }
}