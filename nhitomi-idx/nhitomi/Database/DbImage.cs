using System;
using System.Collections.Generic;
using MessagePack;
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

        [Key("no"), Object(Name = "no", Enabled = false)]
        public DbImageNote[] Notes { get; set; }

        Dictionary<ImageTag, string[]> _tags = new Dictionary<ImageTag, string[]>();

        public string[] GetTags(ImageTag tag) => _tags.GetValueOrDefault(tag);
        public void SetTags(ImageTag tag, string[] value) => _tags[tag] = value;

        [Key("tg"), Text(Name = "tg")]
        public string[] TagsGeneral
        {
            get => GetTags(ImageTag.Tag);
            set => SetTags(ImageTag.Tag, value);
        }

        [Key("ta"), Text(Name = "ta")]
        public string[] TagsArtist
        {
            get => GetTags(ImageTag.Artist);
            set => SetTags(ImageTag.Artist, value);
        }

        [Key("tc"), Text(Name = "tc")]
        public string[] TagsCharacter
        {
            get => GetTags(ImageTag.Character);
            set => SetTags(ImageTag.Character, value);
        }

        [Key("tcp"), Text(Name = "tcp")]
        public string[] TagsCopyright
        {
            get => GetTags(ImageTag.Copyright);
            set => SetTags(ImageTag.Copyright, value);
        }

        [Key("tm"), Text(Name = "tm")]
        public string[] TagsMetadata
        {
            get => GetTags(ImageTag.Metadata);
            set => SetTags(ImageTag.Metadata, value);
        }

        [Key("tp"), Text(Name = "tp")]
        public string[] TagsPool
        {
            get => GetTags(ImageTag.Pool);
            set => SetTags(ImageTag.Pool, value);
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
            Notes       = model.Notes?.ToArray(p => new DbImageNote().Apply(p));
            _tags       = model.Tags?.DictClone() ?? new Dictionary<ImageTag, string[]>();
            Rating      = model.Rating;

            // do not map source because Data is valid only for the scraper that initialized it
        }

#region Cached

        /// <summary>
        /// This is a cached property for querying.
        /// </summary>
        [IgnoreMember, Completion(Name = "sug", PreserveSeparators = false, PreservePositionIncrements = false, DocValues = false), DbCached]
        public CompletionField Suggest { get; set; }

        public override void UpdateCache()
        {
            base.UpdateCache();

            Suggest = new CompletionField
            {
                Input = SuggestionFormatter.Format(new Dictionary<int, string[]>().Chain(d =>
                {
                    foreach (var (key, value) in _tags)
                        d[(int) key] = value;
                }))

                //todo: score
                //Weight = (int) TotalAvailability.Average()
            };
        }

#endregion

        public static implicit operator nhitomiObject(DbImage image) => new nhitomiObject(ObjectType.Image, image.Id);
    }
}