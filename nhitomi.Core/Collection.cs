using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class Collection
    {
        [Key] public int Id { get; set; }

        /// <summary>
        /// Name of this collection.
        /// </summary>
        [Required, MinLength(1), MaxLength(32)]
        public string Name { get; set; }

        public CollectionSort Sort { get; set; }
        public bool SortDescending { get; set; }

        public ulong OwnerId { get; set; }
        public User Owner { get; set; }

        public ICollection<CollectionRef> Doujins { get; set; }

        public static void Describe(ModelBuilder model)
        {
            var entity = model.Entity<Collection>();

            entity.HasIndex(c => c.Name);

            entity
                .HasOne(c => c.Owner)
                .WithMany(u => u.Collections)
                .HasForeignKey(c => c.OwnerId);

            CollectionRef.Describe(model);
        }
    }

    /// <summary>
    /// Join table
    /// </summary>
    public class CollectionRef
    {
        public int CollectionId { get; set; }
        public Collection Collection { get; set; }

        public int DoujinId { get; set; }
        public Doujin Doujin { get; set; }

        public static void Describe(ModelBuilder model)
        {
            var entity = model.Entity<CollectionRef>();

            entity.HasKey(x => new {x.CollectionId, x.DoujinId});

            entity
                .HasOne(x => x.Doujin)
                .WithMany(d => d.Collections)
                .HasForeignKey(x => x.DoujinId);

            entity
                .HasOne(x => x.Collection)
                .WithMany(c => c.Doujins)
                .HasForeignKey(x => x.CollectionId);
        }
    }
}