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
        [IgnoreMember, Keyword(Name = "h", Index = false)] // for elasticsearch
        public string HashString
        {
            get => WebEncoders.Base64UrlEncode(Hash);
            set => Hash = WebEncoders.Base64UrlDecode(value);
        }

        [Key("no"), Object(Name = "no", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

        Dictionary<ImageTag, string[]> _tags = new Dictionary<ImageTag, string[]>();

        [Key("tg"), Keyword(Name = "tg", DocValues = false)]
        public string[] TagsGeneral
        {
            get => _tags.GetValueOrDefault(ImageTag.Tag);
            set => _tags[ImageTag.Tag] = value;
        }

        [Key("ta"), Keyword(Name = "ta", DocValues = false)]
        public string[] TagsArtist
        {
            get => _tags.GetValueOrDefault(ImageTag.Artist);
            set => _tags[ImageTag.Artist] = value;
        }

        [Key("tc"), Keyword(Name = "tc", DocValues = false)]
        public string[] TagsCharacter
        {
            get => _tags.GetValueOrDefault(ImageTag.Character);
            set => _tags[ImageTag.Character] = value;
        }

        [Key("tcp"), Keyword(Name = "tcp", DocValues = false)]
        public string[] TagsCopyright
        {
            get => _tags.GetValueOrDefault(ImageTag.Copyright);
            set => _tags[ImageTag.Copyright] = value;
        }

        [Key("tm"), Keyword(Name = "tm", DocValues = false)]
        public string[] TagsMetadata
        {
            get => _tags.GetValueOrDefault(ImageTag.Metadata);
            set => _tags[ImageTag.Metadata] = value;
        }

        [Key("tp"), Keyword(Name = "tp", DocValues = false)]
        public string[] TagsPool
        {
            get => _tags.GetValueOrDefault(ImageTag.Pool);
            set => _tags[ImageTag.Pool] = value;
        }

        [Key("ra"), Keyword(Name = "ra", DocValues = false)]
        public MaterialRating Rating { get; set; }

        [Key("sr"), Keyword(Name = "sr", DocValues = false)]
        public ScraperType Source { get; set; }

        [Key("si"), Keyword(Name = "si", DocValues = false)]
        public string SourceId { get; set; }

        /// <summary>
        /// Cannot query against this property.
        /// </summary>
        [Key("da"), Keyword(Name = "da", Index = false)]
        public string Data { get; set; }

        public override void MapTo(Image model)
        {
            base.MapTo(model);

            model.CreatedTime = CreatedTime;
            model.UpdatedTime = UpdatedTime;
            model.Size        = Size;
            model.Hash        = Hash;
            model.Notes       = Notes?.ToArray(n => n.Convert());
            model.Tags        = _tags.DictClone();
            model.Rating      = Rating;
            model.Source      = Source;
        }

        public override void MapFrom(Image model)
        {
            base.MapFrom(model);

            CreatedTime = model.CreatedTime;
            UpdatedTime = model.UpdatedTime;
            Size        = model.Size;
            Hash        = model.Hash;
            Notes       = model.Notes?.ToArray(p => new DbImageNote().Apply(p));
            _tags       = model.Tags?.DictClone() ?? new Dictionary<ImageTag, string[]>();
            Rating      = model.Rating;

            // do not map source because Data is valid only for the scraper that initialized it
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = IDbSupportsAutocomplete.SuggestField, PreserveSeparators = false, PreservePositionIncrements = false, DocValues = false), SanitizerIgnore]
        public CompletionField Suggest { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(new Dictionary<int, string[]>().Compose(d =>
                {
                    foreach (var (key, value) in _tags)
                        d[(int) key] = value;

                    return d;
                }))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbImage image) => new nhitomiObject(ObjectType.Image, image.Id);
    }
}