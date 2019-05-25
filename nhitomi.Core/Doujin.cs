using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace nhitomi.Core
{
    public class Doujin
    {
        [Key] public int Id { get; set; }

        /// <summary>
        /// Prettified name of the doujinshi.
        /// This is often English.
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

        public string Scanlator { get; set; }
        public string Language { get; set; }
        public string ParodyOf { get; set; }

        public ICollection<string> Characters { get; set; }
        public ICollection<string> Categories { get; set; }
        public ICollection<string> Artists { get; set; }
        public ICollection<string> Groups { get; set; }
        public ICollection<string> Tags { get; set; }

        /// <summary>
        /// Gets the pages of this doujinshi.
        /// </summary>
        public ICollection<Page> Pages { get; }

        public static void Describe(EntityTypeBuilder<Doujin> entity)
        {
            entity.HasIndex(d => d.PrettyName);
            entity.HasIndex(d => d.OriginalName);
        }
    }

    public sealed class Page
    {
        [Key] public int Id { get; set; }

        /// <summary>
        /// Doujin navigation property.
        /// </summary>
        public Doujin Doujin { get; set; }

        public int DoujinId { get; set; }

        [Required] public string Url { get; set; }

        public static void Describe(EntityTypeBuilder<Page> entity)
        {
            entity
                .HasOne(p => p.Doujin)
                .WithMany(d => d.Pages)
                .HasForeignKey(p => p.DoujinId)
                .IsRequired();
        }
    }
}