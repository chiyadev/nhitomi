using MessagePack;
using Microsoft.AspNetCore.WebUtilities;
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
        [Key("s"), Number(Name = "s")]
        public int? Size { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("h"), Ignore] // for msgpack
        public byte[] Hash { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [IgnoreMember, Keyword(Name = "h", Index = false)] // for elasticsearch
        public string HashString
        {
            get => WebEncoders.Base64UrlEncode(Hash);
            set => Hash = WebEncoders.Base64UrlDecode(value);
        }

        [Key("n"), Object(Name = "n", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

        public override void MapTo(BookImage model)
        {
            base.MapTo(model);

            model.Size = Size;
            model.Hash = Hash;

            model.Notes = Notes?.ToArray(n => n.Convert());
        }

        public override void MapFrom(BookImage model)
        {
            base.MapFrom(model);

            Size = model.Size;
            Hash = model.Hash;

            Notes = model.Notes?.ToArray(p => new DbImageNote().Apply(p));
        }
    }
}