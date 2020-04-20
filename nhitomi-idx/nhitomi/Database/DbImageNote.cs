using System;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents a note on an image.
    /// Notes can be used to annotate images with text, usually for translation.
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(ImageNote))]
    public class DbImageNote : DbObjectBase<ImageNote>, IDbModelConvertible<DbImageNote, ImageNote, ImageNoteBase>, IHasUpdatedTime
    {
        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// Hash of combined piece list which can be found using <see cref="IDbSupportsPieces.GetCombinedPieceHash"/>.
        /// </summary>
        [Key("ha"), Keyword(Name = "ha")]
        public string TargetHash { get; set; }

        [Key("x"), Number(Name = "x")]
        public int X { get; set; }

        [Key("y"), Number(Name = "y")]
        public int Y { get; set; }

        [Key("w"), Number(Name = "w")]
        public int Width { get; set; }

        [Key("h"), Number(Name = "h")]
        public int Height { get; set; }

        [Key("c"), Text(Name = "c")]
        public string Content { get; set; }

        public override void MapTo(ImageNote model)
        {
            base.MapTo(model);

            model.X       = X;
            model.Y       = Y;
            model.Width   = Width;
            model.Height  = Height;
            model.Content = Content;
        }

        public override void MapFrom(ImageNote model)
        {
            base.MapFrom(model);

            X       = model.X;
            Y       = model.Y;
            Width   = model.Width;
            Height  = model.Height;
            Content = model.Content;
        }
    }
}