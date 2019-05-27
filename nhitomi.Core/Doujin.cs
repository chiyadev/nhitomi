using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
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

        public Scanlator Scanlator { get; set; }
        public Language Language { get; set; }
        public ParodyOf ParodyOf { get; set; }

        public ICollection<Character> Characters { get; set; }
        public ICollection<Category> Categories { get; set; }
        public ICollection<Artist> Artists { get; set; }
        public ICollection<Group> Groups { get; set; }
        public ICollection<Tag> Tags { get; set; }

        /// <summary>
        /// Gets the pages of this doujinshi.
        /// </summary>
        public ICollection<Page> Pages { get; set; }

        public static void Describe(ModelBuilder model)
        {
            var entity = model.Entity<Doujin>();

            entity.HasIndex(d => d.PrettyName);
            entity.HasIndex(d => d.OriginalName);

            entity.HasIndex(d => d.Source);
            entity.HasIndex(d => d.SourceId);

            Scanlator.Describe(model, d => d.Scanlator);
            Language.Describe(model, d => d.Language);
            ParodyOf.Describe(model, d => d.ParodyOf);

            Character.Describe(model, d => d.Characters);
            Category.Describe(model, d => d.Categories);
            Artist.Describe(model, d => d.Artists);
            Group.Describe(model, d => d.Groups);
            Tag.Describe(model, d => d.Tags);

            Page.Describe(model);
        }
    }

    /// <summary>
    /// Base class that represents a metadata entry.
    /// Many-to-many is not used for performance.
    /// </summary>
    public abstract class MetadataBase<TEntity>
        where TEntity : MetadataBase<TEntity>
    {
        [Key] public int Id { get; set; }

        public Doujin Doujin { get; set; }
        public int DoujinId { get; set; }

        [Required] public string Value { get; set; }

        static void Describe(ModelBuilder model)
        {
            model.Entity<TEntity>().HasIndex(t => t.Value);
        }

        public static void Describe(ModelBuilder model, Expression<Func<Doujin, TEntity>> path)
        {
            Describe(model);

            model.Entity<TEntity>()
                .HasOne(t => t.Doujin)
                .WithOne(path)
                .HasForeignKey<TEntity>(t => t.DoujinId)
                .IsRequired(false);
        }

        public static void Describe(ModelBuilder model, Expression<Func<Doujin, IEnumerable<TEntity>>> path)
        {
            Describe(model);

            model.Entity<TEntity>()
                .HasOne(t => t.Doujin)
                .WithMany(path)
                .HasForeignKey(t => t.DoujinId)
                .IsRequired();
        }
    }

    public class Scanlator : MetadataBase<Scanlator>
    {
    }

    public class Language : MetadataBase<Language>
    {
    }

    public class ParodyOf : MetadataBase<ParodyOf>
    {
    }

    public class Character : MetadataBase<Character>
    {
    }

    public class Category : MetadataBase<Category>
    {
    }

    public class Artist : MetadataBase<Artist>
    {
    }

    public class Group : MetadataBase<Group>
    {
    }

    public class Tag : MetadataBase<Tag>
    {
    }

    public class Page
    {
        [Key] public int Id { get; set; }

        public Doujin Doujin { get; set; }
        public int DoujinId { get; set; }

        [Required] public string Url { get; set; }

        public static void Describe(ModelBuilder model)
        {
            var entity = model.Entity<Page>();

            entity
                .HasOne(p => p.Doujin)
                .WithMany(d => d.Pages)
                .HasForeignKey(p => p.DoujinId)
                .IsRequired();
        }
    }
}