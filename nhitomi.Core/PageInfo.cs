using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace nhitomi.Core
{
    public sealed class PageInfo
    {
        [Key] public int Id { get; set; }

        /// <summary>
        /// Doujin navigation property.
        /// </summary>
        public DoujinInfo Doujin { get; set; }

        public int DoujinId { get; set; }

        [Required] public string Url { get; set; }

        public static void Describe(EntityTypeBuilder<PageInfo> entity)
        {
            entity
                .HasOne(p => p.Doujin)
                .WithMany(d => d.Pages)
                .HasForeignKey(p => p.DoujinId)
                .IsRequired();
        }
    }
}