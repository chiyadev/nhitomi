using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public static class DoujinTag
    {
        public const int MinLength = 1;
        public const int MaxLength = 32;
    }

    /// <summary>
    /// Tag is a property of doujinshi which can be attached multiple times.
    /// Many-to-many is used.
    /// </summary>
    public abstract class TagBase<TTag, TJoin>
        where TTag : TagBase<TTag, TJoin>, new()
        where TJoin : TagBase<TTag, TJoin>.JoinBase
    {
        [Key] public int Id { get; set; }

        public ICollection<TJoin> Doujins { get; set; }

        [Required, MinLength(DoujinTag.MinLength), MaxLength(DoujinTag.MaxLength)]
        public string Value { get; set; }

        /// <summary>
        /// Join table
        /// </summary>
        public abstract class JoinBase
        {
            public int DoujinId { get; set; }
            public Doujin Doujin { get; set; }

            public int TagId { get; set; }
            public TTag Tag { get; set; }

            protected JoinBase()
            {
            }

            protected JoinBase(string value)
            {
                Tag = new TTag
                {
                    Value = value
                };
            }
        }

        public static void Describe(ModelBuilder model, Expression<Func<Doujin, IEnumerable<TJoin>>> path)
        {
            model.Entity<TTag>(tag =>
            {
                // unique index for tag value
                tag.HasIndex(m => m.Value).IsUnique();
            });

            model.Entity<TJoin>(join =>
            {
                join.ToTable($"{typeof(TTag).Name}Refs");

                join.HasKey(x => new {x.DoujinId, x.TagId});

                join.HasOne(x => x.Doujin)
                    .WithMany(path)
                    .HasForeignKey(x => x.DoujinId);

                join.HasOne(x => x.Tag)
                    .WithMany(t => t.Doujins)
                    .HasForeignKey(x => x.TagId);
            });
        }
    }

    public class Character : TagBase<Character, Character.Reference>
    {
        public class Reference : JoinBase
        {
            public Reference()
            {
            }

            public Reference(string value) : base(value)
            {
            }
        }
    }

    public class Category : TagBase<Category, Category.Reference>
    {
        public class Reference : JoinBase
        {
            public Reference()
            {
            }

            public Reference(string value) : base(value)
            {
            }
        }
    }

    public class Tag : TagBase<Tag, Tag.Reference>
    {
        public class Reference : JoinBase
        {
            public Reference()
            {
            }

            public Reference(string value) : base(value)
            {
            }
        }
    }
}