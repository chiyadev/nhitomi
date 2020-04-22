using System;
using System.Collections.Generic;
using MessagePack;
using Microsoft.AspNetCore.WebUtilities;
using Nest;
using nhitomi.Models;
using nhitomi.Scrapers;

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
        [IgnoreMember, Keyword(Name = "h", DocValues = false, Index = false)] // for elasticsearch
        public string HashString
        {
            get => WebEncoders.Base64UrlEncode(Hash);
            set => Hash = WebEncoders.Base64UrlDecode(value);
        }

        [Key("no"), Object(Name = "no", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

        [Key("tg"), Keyword(Name = "tg", DocValues = false)]
        public string[] TagsGeneral { get; set; }

        [Key("ta"), Keyword(Name = "ta", DocValues = false)]
        public string[] TagsArtist { get; set; }

        [Key("tc"), Keyword(Name = "tc", DocValues = false)]
        public string[] TagsCharacter { get; set; }

        [Key("tcp"), Keyword(Name = "tcp", DocValues = false)]
        public string[] TagsCopyright { get; set; }

        [Key("tm"), Keyword(Name = "tm", DocValues = false)]
        public string[] TagsMetadata { get; set; }

        [Key("tp"), Keyword(Name = "tp", DocValues = false)]
        public string[] TagsPool { get; set; }

        [Key("ra"), Keyword(Name = "ra", DocValues = false)]
        public MaterialRating Rating { get; set; }

        [Key("sr"), Keyword(Name = "sr", DocValues = false)]
        public ScraperType Source { get; set; }

        [Key("da"), Keyword(Name = "da", DocValues = false, Index = false)]
        public string Data { get; set; }

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

            model.Rating = Rating;
            model.Source = Source;
        }

        public override void MapFrom(Image model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;

            Size = model.Size;
            Hash = model.Hash;

            Notes = model.Notes?.ToArray(p => new DbImageNote().Apply(p));

            TagsGeneral   = model.Tags?.GetValueOrDefault(ImageTag.Tag);
            TagsArtist    = model.Tags?.GetValueOrDefault(ImageTag.Artist);
            TagsCharacter = model.Tags?.GetValueOrDefault(ImageTag.Character);
            TagsCopyright = model.Tags?.GetValueOrDefault(ImageTag.Copyright);
            TagsMetadata  = model.Tags?.GetValueOrDefault(ImageTag.Metadata);
            TagsPool      = model.Tags?.GetValueOrDefault(ImageTag.Pool);

            Rating = model.Rating;
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
        [IgnoreMember, Completion(Name = IDbSupportsAutocomplete.SuggestField, PreserveSeparators = false, PreservePositionIncrements = false, DocValues = false)]
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