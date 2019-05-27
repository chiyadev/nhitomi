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
        [Timestamp]
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

        public Artist Artist { get; set; }
        public Group Group { get; set; }
        public Scanlator Scanlator { get; set; }
        public Language Language { get; set; }
        public ParodyOf ParodyOf { get; set; }

        public ICollection<Character.Reference> Characters { get; set; }
        public ICollection<Category.Reference> Categories { get; set; }
        public ICollection<Tag.Reference> Tags { get; set; }

        /// <summary>
        /// Gets the pages of this doujinshi.
        /// </summary>
        public ICollection<Page> Pages { get; set; }

        /// <summary>
        /// Gets the collections that contain this doujin.
        /// This is for navigation only and should not be included in queries.
        /// </summary>
        public ICollection<DoujinCollection> Collections { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Doujin>(doujin =>
            {
                doujin.HasIndex(d => d.PrettyName);
                doujin.HasIndex(d => d.OriginalName);

                doujin.HasIndex(d => d.Source);
                doujin.HasIndex(d => d.SourceId);
            });

            Artist.Describe(model, d => d.Artist);
            Group.Describe(model, d => d.Group);
            Scanlator.Describe(model, d => d.Scanlator);
            Language.Describe(model, d => d.Language);
            ParodyOf.Describe(model, d => d.ParodyOf);

            Character.Describe(model, d => d.Characters);
            Category.Describe(model, d => d.Categories);
            Tag.Describe(model, d => d.Tags);

            Page.Describe(model);
        }
    }

    public class Page
    {
        [Key] public int Id { get; set; }

        public Doujin Doujin { get; set; }
        public int DoujinId { get; set; }

        [Required] public string Url { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Page>(page =>
            {
                page.HasOne(p => p.Doujin)
                    .WithMany(d => d.Pages)
                    .HasForeignKey(p => p.DoujinId)
                    .IsRequired();
            });
        }
    }

    public static class DoujinQueryExtensions
    {
        public static IQueryable<Doujin> IncludeRelated(this IQueryable<Doujin> queryable) => queryable
            .Include(d => d.Artist)
            .Include(d => d.Group)
            .Include(d => d.Scanlator)
            .Include(d => d.Language)
            .Include(d => d.ParodyOf)
            .Include(d => d.Characters).ThenInclude(x => x.Tag)
            .Include(d => d.Categories).ThenInclude(x => x.Tag)
            .Include(d => d.Tags).ThenInclude(x => x.Tag)
            .Include(d => d.Pages);
    }
}