using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public enum FeedChannelWhitelistType
    {
        /// <summary>
        /// Doujin must have all tags specified for a feed channel.
        /// </summary>
        All,

        /// <summary>
        /// Doujin must have any of the tags specified for a feed channel.
        /// </summary>
        Any
    }

    public class FeedChannel
    {
        [Key] public ulong Id { get; set; }

        public Guild Guild { get; set; }
        public ulong GuildId { get; set; }

        public Doujin LastDoujin { get; set; }
        public int LastDoujinId { get; set; }

        public ICollection<FeedChannelTag> Tags { get; set; }

        public FeedChannelWhitelistType WhitelistType { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<FeedChannel>(channel =>
            {
                channel
                    .HasOne(c => c.LastDoujin)
                    .WithMany(d => d.FeedChannels)
                    .HasForeignKey(c => c.LastDoujinId);

                channel
                    .HasOne(c => c.Guild)
                    .WithMany(g => g.FeedChannels)
                    .HasForeignKey(c => c.GuildId);
            });

            model.Entity<FeedChannelTag>(join =>
            {
                join.HasKey(x => new {x.FeedChannelId, x.TagId});

                join.HasOne(x => x.FeedChannel)
                    .WithMany(c => c.Tags)
                    .HasForeignKey(x => x.FeedChannelId);

                join.HasOne(x => x.Tag)
                    .WithMany(t => t.FeedChannels)
                    .HasForeignKey(x => x.TagId);
            });
        }
    }

    /// <summary>
    /// Join table
    /// </summary>
    public class FeedChannelTag
    {
        public ulong FeedChannelId { get; set; }
        public FeedChannel FeedChannel { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}