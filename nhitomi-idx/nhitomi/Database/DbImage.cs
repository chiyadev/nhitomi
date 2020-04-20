using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents an image in an imageboard/booru (synonymous to "post").
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Image))]
    public class DbImage : DbObjectBase<Image>, IDbModelConvertible<DbImage, Image, ImageBase>, IHasUpdatedTime, IDbSupportsSnapshot, IDbSupportsPieces
    {
        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

        [Key("sw"), Number(Name = "sw")]
        public int Width { get; set; }

        [Key("sh"), Number(Name = "sh")]
        public int Height { get; set; }

        [Key("pi"), Object(Name = "pi", Enabled = false)]
        public DbPiece[] Pieces { get; set; }

        [Key("tg"), Text(Name = "tg")]
        public string[] TagsGeneral { get; set; }

        [Key("ta"), Text(Name = "ta")]
        public string[] TagsArtist { get; set; }

        [Key("tc"), Text(Name = "tc")]
        public string[] TagsCharacter { get; set; }

        [Key("tcp"), Text(Name = "tcp")]
        public string[] TagsCopyright { get; set; }

        [Key("tm"), Text(Name = "tm")]
        public string[] TagsMetadata { get; set; }

        [Key("tp"), Text(Name = "tp")]
        public string[] TagsPool { get; set; }

        [Key("sr"), Keyword(Name = "sr")]
        public string[] Sources { get; set; }

        [Key("ra"), Keyword(Name = "ra")]
        public MaterialRating Rating { get; set; }

        public override void MapTo(Image model)
        {
            base.MapTo(model);

            model.CreatedTime = CreatedTime;
            model.UpdatedTime = UpdatedTime;
            model.Width       = Width;
            model.Height      = Height;

            model.Pieces = Pieces?.ToArray(p => p.Convert());

            model.Tags = new Dictionary<ImageTag, string[]>
            {
                [ImageTag.Tag]       = TagsGeneral,
                [ImageTag.Artist]    = TagsArtist,
                [ImageTag.Character] = TagsCharacter,
                [ImageTag.Copyright] = TagsCopyright,
                [ImageTag.Metadata]  = TagsMetadata,
                [ImageTag.Pool]      = TagsPool
            };

            model.Sources = Sources?.ToArray(WebsiteSource.Parse);
            model.Rating  = Rating;
        }

        public override void MapFrom(Image model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            Width       = model.Width;
            Height      = model.Height;

            Pieces = model.Pieces?.ToArray(p => new DbPiece().Apply(p));

            if (model.Tags != null)
            {
                TagsGeneral   = model.Tags.GetValueOrDefault(ImageTag.Tag);
                TagsArtist    = model.Tags.GetValueOrDefault(ImageTag.Artist);
                TagsCharacter = model.Tags.GetValueOrDefault(ImageTag.Character);
                TagsCopyright = model.Tags.GetValueOrDefault(ImageTag.Copyright);
                TagsMetadata  = model.Tags.GetValueOrDefault(ImageTag.Metadata);
                TagsPool      = model.Tags.GetValueOrDefault(ImageTag.Pool);
            }

            Sources = model.Sources?.ToArray(s => s.ToString());
            Rating  = model.Rating;
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Number(Name = "sz")]
        public int Size { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            Size = Pieces?.Sum(p => p.Size) ?? 0;
        }

#endregion

        [IgnoreMember, Ignore]
        public SnapshotTarget SnapshotTarget => SnapshotTarget.Image;

        public static implicit operator nhitomiObject(DbImage image) => new nhitomiObject(image.SnapshotTarget, image.Id);
    }
}