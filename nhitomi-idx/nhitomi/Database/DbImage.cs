using System;
using System.Collections.Generic;
using MessagePack;
using Microsoft.AspNetCore.WebUtilities;
using Nest;
using nhitomi.Models;

namespace nhitomi.Database
{
    /// <summary>
    /// Represents an image in an imageboard/booru (synonymous to "post").
    /// </summary>
    [MessagePackObject, ElasticsearchType(RelationName = nameof(Image))]
    public class DbImage : DbObjectBase<Image>, IDbHasType, IDbModelConvertible<DbImage, Image, ImageBase>, IHasUpdatedTime, IDbSupportsAutocomplete
    {
        [IgnoreMember, Ignore]
        ObjectType IDbHasType.Type => ObjectType.Image;

        [Key("Tc"), Date(Name = "Tc")]
        public DateTime CreatedTime { get; set; }

        [Key("Tu"), Date(Name = "Tu")]
        public DateTime UpdatedTime { get; set; }

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

        [Key("no"), Object(Name = "no", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

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

            model.Size = Size;
            model.Hash = Hash;

            model.Notes = Notes?.ToArray(n => n.Convert());

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

            Size = model.Size;
            Hash = model.Hash;

            Notes = model.Notes?.ToArray(p => new DbImageNote().Apply(p));

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

        public enum SuggestionType
        {
            Tags = ImageTag.Tag,
            TagsArtist = ImageTag.Artist,
            TagsCharacter = ImageTag.Character,
            TagsCopyright = ImageTag.Copyright,
            TagsMetadata = ImageTag.Metadata,
            TagsPool = ImageTag.Pool
        }

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = IDbSupportsAutocomplete.SuggestField, PreserveSeparators = false, PreservePositionIncrements = false)]
        public CompletionField Suggest { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(
                    (SuggestionType.Tags, TagsGeneral),
                    (SuggestionType.TagsArtist, TagsArtist),
                    (SuggestionType.TagsCharacter, TagsCharacter),
                    (SuggestionType.TagsCopyright, TagsCopyright),
                    (SuggestionType.TagsMetadata, TagsMetadata),
                    (SuggestionType.TagsPool, TagsPool))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbImage image) => new nhitomiObject(ObjectType.Image, image.Id);
    }
}