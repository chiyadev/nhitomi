using MessagePack;
using Microsoft.AspNetCore.WebUtilities;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a piece of file.
    /// </summary>
    [MessagePackObject]
    public class DbPiece : IDbModelConvertible<DbPiece, Piece>
    {
        [Key("s"), Number(Name = "s")]
        public int Size { get; set; }

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

        public void MapTo(Piece model)
        {
            model.Size = Size;
            model.Hash = Hash;
        }

        public void MapFrom(Piece model)
        {
            Size = model.Size;
            Hash = model.Hash;
        }
    }
}