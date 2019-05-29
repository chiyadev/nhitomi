using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public enum TagType
    {
        Artist,
        Group,
        Scanlator,
        Language,
        Parody,
        Character,
        Category,
        Tag
    }

    public class Tag
    {
        public int Id { get; set; }
        public TagType Type { get; set; }

        public ICollection<TagRef> Doujins { get; set; }

        [Required, MinLength(1), MaxLength(32)]
        public string Value { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Tag>(tag =>
            {
                // composite key consisting of id and type
                tag.HasKey(t => new {t.Id, t.Type});

                // id is automatic
                tag.Property(t => t.Id).ValueGeneratedOnAdd();
            });

            model.Entity<TagRef>(join =>
            {
                join.HasKey(x => new {x.DoujinId, x.TagId});

                join.HasOne(x => x.Doujin)
                    .WithMany(d => d.Tags)
                    .HasForeignKey(x => x.DoujinId);

                join.HasOne(x => x.Tag)
                    .WithMany(t => t.Doujins)
                    .HasForeignKey(x => new {x.TagId, x.TagType});
            });
        }
    }

    /// <summary>
    /// Join table
    /// </summary>
    public class TagRef
    {
        public int DoujinId { get; set; }
        public Doujin Doujin { get; set; }

        public int TagId { get; set; }
        public TagType TagType { get; set; }
        public Tag Tag { get; set; }

        public TagRef()
        {
        }

        public TagRef(TagType type, string value)
        {
            Tag = new Tag
            {
                Type = type,
                Value = value
            };
        }
    }
}