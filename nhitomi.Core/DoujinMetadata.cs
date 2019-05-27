using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    /// <summary>
    /// Metadata is a property of a doujinshi which can be specified only once.
    /// One-to-many is used.
    /// </summary>
    public abstract class MetadataBase<TMetadata>
        where TMetadata : MetadataBase<TMetadata>
    {
        [Key] public int Id { get; set; }

        public ICollection<Doujin> Doujins { get; set; }

        [Required, MinLength(DoujinTag.MinLength), MaxLength(DoujinTag.MaxLength)]
        public string Value { get; set; }

        public static void Describe(ModelBuilder model, Expression<Func<Doujin, TMetadata>> path)
        {
            model.Entity<TMetadata>(meta =>
            {
                meta.HasMany(m => m.Doujins)
                    .WithOne(path)
                    .IsRequired(false);

                meta.HasIndex(m => m.Value).IsUnique();
            });
        }
    }

    public class Artist : MetadataBase<Artist>
    {
    }

    public class Group : MetadataBase<Group>
    {
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
}