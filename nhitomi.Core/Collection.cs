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

        public ICollection<CollectionRef> Doujins { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Collection>(collection => { collection.HasIndex(c => c.Name); });

            model.Entity<CollectionRef>(join =>
            {
                join.HasKey(x => new {x.CollectionId, x.DoujinId});

                join
                    .HasOne(x => x.Doujin)
                    .WithMany(d => d.Collections)
                    .HasForeignKey(x => x.DoujinId);

                join
                    .HasOne(x => x.Collection)
                    .WithMany(c => c.Doujins)
                    .HasForeignKey(x => x.CollectionId);
            });
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
    }
}