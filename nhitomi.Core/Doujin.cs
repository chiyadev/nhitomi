using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class Doujin
    {
        [Key] public int Id { get; set; }

        /// <summary>
        /// The URL at which this doujinshi was initially found at.
        /// </summary>
        [Required]
        public string GalleryUrl { get; set; }

        /// <summary>
        /// Prettified name of the doujinshi.
        /// This is usually English.
        /// </summary>
        [Required]
        public string PrettyName { get; set; }

        /// <summary>
        /// Original name of the doujinshi.
        /// This is usually the original language of the doujinshi (i.e. Japanese).
        /// </summary>
        [Required]
        public string OriginalName { get; set; }

        /// <summary>
        /// Original name or pretty name.
        /// This property is not mapped and should not be used in queries.
        /// </summary>
        [NotMapped]
        public string Name => OriginalName ?? PrettyName;

        /// <summary>
        /// The time at which this doujinshi was uploaded.
        /// </summary>
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// The time at which this doujinshi object was created/processed.
        /// </summary>
        public DateTime ProcessTime { get; set; }

        /// <summary>
        /// The source of this doujinshi (e.g. nhentai, hitomi, etc.).
        /// </summary>
        [Required]
        public string Source { get; set; }

        /// <summary>
        /// The identifier used by the source (e.g. gallery ID for nhentai).
        /// </summary>
        [Required]
        public string SourceId { get; set; }

        /// <summary>
        /// Internal data used to store <see cref="IDoujinClient"/>-specific information, such as page information.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Number of pages in this doujin.
        /// </summary>
        public int PageCount { get; set; }

        public ICollection<TagRef> Tags { get; set; }

        /// <summary>
        /// Gets the collections that contain this doujin.
        /// This is for navigation only and should not be included in queries.
        /// </summary>
        public ICollection<CollectionRef> Collections { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Doujin>(doujin =>
            {
                doujin.HasIndex(d => d.PrettyName);
                doujin.HasIndex(d => d.OriginalName);

                doujin.HasIndex(d => d.Source);
                doujin.HasIndex(d => d.SourceId);
            });

            Tag.Describe(model);
        }
    }

    public static class DoujinExtensions
    {
        public static IQueryable<Doujin> IncludeRelated(this IQueryable<Doujin> queryable) => queryable
            .Include(d => d.Tags).ThenInclude(x => x.Tag);

        public static Tag GetTag(this Doujin doujin, TagType type) =>
            doujin.Tags.Select(x => x.Tag).FirstOrDefault(x => x.Type == type);

        public static Tag[] GetTags(this Doujin doujin, TagType type) =>
            doujin.Tags.Select(x => x.Tag).Where(t => t.Type == type).ToArray();
    }
}
