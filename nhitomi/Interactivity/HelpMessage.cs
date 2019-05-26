using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public enum HelpMessageSection
    {
        Doujins,
        Collections,
        Tags,
        Other
    }

    public class HelpMessage : ListInteractiveMessage<HelpMessageSection>
    {
        public HelpMessage() : base(new EnumerableBrowser<HelpMessageSection>(
            Enum.GetValues(typeof(HelpMessageSection)).Cast<HelpMessageSection>()))
        {
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        protected override Embed CreateEmbed(HelpMessageSection value)
        {
            var settings = Services.GetRequiredService<IOptions<AppSettings>>().Value;
            var prefix = settings.Discord.Prefix;

            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithDescription(
                    "nhitomi — a Discord bot for searching and downloading doujinshi, by **chiya.dev** - https://chiya.dev\n" +
                    $"Official server: {settings.Discord.Guild.GuildInvite}")
                .WithColor(Color.Purple)
                .WithThumbnailUrl(settings.ImageUrl)
                .WithCurrentTimestamp();

            switch (value)
            {
                case HelpMessageSection.Doujins:
                    embed.AddField("  — Doujinshi —", $@"
- {prefix}get `source` `id` — Displays doujin information from a source by its ID.
- {prefix}all `source` — Displays all doujins from a source uploaded recently.
- {prefix}search `query` — Searches for doujins by the title and tags that satisfy your query.
- {prefix}download `source` `id` — Sends a download link for a doujin by its ID.
".Trim());
                    break;

                case HelpMessageSection.Collections:
                    embed.AddField("  — Collection management —", $@"
- {prefix}collection — Lists all collections belonging to you.
- {prefix}collection `name` — Displays doujins belonging to a collection.
- {prefix}collection `name` add|remove `source` `id` — Adds or removes a doujin in a collection.
- {prefix}collection `name` list — Lists all doujins belonging to a collection.
- {prefix}collection `name` sort `attribute` — Sorts doujins in a collection by an attribute ({string.Join(", ", Enum.GetNames(typeof(CollectionSortAttribute)).Select(s => s.ToLowerInvariant()))}).
- {prefix}collection `name` delete — Deletes a collection, removing all doujins belonging to it.
".Trim());
                    break;

                case HelpMessageSection.Tags:
                    embed.AddField("  — Tag subscriptions —", $@"
- {prefix}subscription — Lists all tags you are subscribed to.
- {prefix}subscription add|remove `tag` — Adds or removes a tag subscription.
- {prefix}subscription clear — Removes all tag subscriptions.
".Trim());
                    break;

                case HelpMessageSection.Other:
                    embed.AddField("  — Sources —", @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim())
                        .AddField("  — Contribution —", @"
This project is licensed under the MIT License.
Contributions are welcome! <https://github.com/chiyadev/nhitomi>
".Trim());
                    break;
            }

            return embed.Build();
        }

        protected override Embed CreateEmptyEmbed() => throw new System.NotImplementedException();
    }
}