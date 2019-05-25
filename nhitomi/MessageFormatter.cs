// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Database;
using Newtonsoft.Json;

namespace nhitomi
{
    public class MessageFormatter
    {
        public static IEmote FloppyDiskEmote => new Emoji("\uD83D\uDCBE");
        public static IEmote TrashcanEmote => new Emoji("\uD83D\uDDD1");
        public static IEmote HeartEmote => new Emoji("\u2764");
        public static IEmote LeftArrowEmote => new Emoji("\u25c0");
        public static IEmote RightArrowEmote => new Emoji("\u25b6");

        readonly AppSettings _settings;
        readonly JsonSerializer _json;

        public MessageFormatter(
            IOptions<AppSettings> options,
            JsonSerializer json)
        {
            _settings = options.Value;
            _json = json;
        }

        static string Join(IEnumerable<string> values)
        {
            var array = values?.ToArray();

            return array == null || array.Length == 0
                ? null
                : string.Join(", ", array);
        }

        public Embed CreateDoujinEmbed(Doujin doujin)
        {
            var embed = new EmbedBuilder()
                .WithTitle(doujin.OriginalName ?? doujin.PrettyName)
                .WithDescription(doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName)
                .WithAuthor(a => a
                    .WithName(Join(doujin.Artists?.Select(x => x.Value)) ?? doujin.Source)
                    .WithIconUrl(doujin.Source.IconUrl))
                .WithUrl(doujin.GalleryUrl)
                .WithImageUrl(doujin.Pages.First().Url)
                .WithColor(Color.Green)
                .WithFooter($"{doujin.Source}/{doujin.SourceId}");

            embed.AddFieldSafe("Language", doujin.Language?.Value, true);
            embed.AddFieldSafe("Parody of", doujin.ParodyOf?.Value, true);
            embed.AddFieldSafe("Categories", Join(doujin.Categories?.Select(x => x.Value)), true);
            embed.AddFieldSafe("Characters", Join(doujin.Characters?.Select(x => x.Value)), true);
            embed.AddFieldSafe("Tags", Join(doujin.Tags?.Select(x => x.Value)), true);

            embed.AddField("Content", $"{doujin.Pages.Count} pages", true);

            return embed.Build();
        }

        public async Task AddDoujinTriggersAsync(IUserMessage message, bool isFeed = false)
        {
            await message.AddReactionAsync(HeartEmote);

            if (!isFeed)
            {
                await message.AddReactionsAsync(new[]
                {
                    FloppyDiskEmote,
                    TrashcanEmote
                });
            }
        }

        public Task AddListTriggersAsync(IUserMessage message) =>
            message.AddReactionsAsync(new[]
            {
                LeftArrowEmote,
                RightArrowEmote
            });

        public Embed CreateHelpEmbed() =>
            new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithDescription(
                    "nhitomi — a Discord bot for searching and downloading doujinshi, by **chiya.dev** - https://chiya.dev\n" +
                    $"Official server: {_settings.Discord.Guild.GuildInvite}")
                .AddField("  — Doujinshi —", $@"
- {_settings.Discord.Prefix}get `source` `id` — Displays doujin information from a source by its ID.
- {_settings.Discord.Prefix}all `source` — Displays all doujins from a source uploaded recently.
- {_settings.Discord.Prefix}search `query` — Searches for doujins by the title and tags that satisfy your query.
- {_settings.Discord.Prefix}download `source` `id` — Sends a download link for a doujin by its ID.
".Trim())
                .AddField("  — Tag subscriptions —", $@"
- {_settings.Discord.Prefix}subscription — Lists all tags you are subscribed to.
- {_settings.Discord.Prefix}subscription add|remove `tag` — Adds or removes a tag subscription.
- {_settings.Discord.Prefix}subscription clear — Removes all tag subscriptions.
".Trim())
                .AddField("  — Collection management —", $@"
- {_settings.Discord.Prefix}collection — Lists all collections belonging to you.
- {_settings.Discord.Prefix}collection `name` — Displays doujins belonging to a collection.
- {_settings.Discord.Prefix}collection `name` add|remove `source` `id` — Adds or removes a doujin in a collection.
- {_settings.Discord.Prefix}collection `name` list — Lists all doujins belonging to a collection.
- {_settings.Discord.Prefix}collection `name` sort `attribute` — Sorts doujins in a collection by an attribute ({string.Join(", ", Enum.GetNames(typeof(CollectionSortAttribute)).Select(s => s.ToLowerInvariant()))}).
- {_settings.Discord.Prefix}collection `name` delete — Deletes a collection, removing all doujins belonging to it.
".Trim())
                .AddField("  — Sources —", @"
- nhentai — `https://nhentai.net/`
- hitomi — `https://hitomi.la/`
".Trim())
                .AddField("  — Contribution —", @"
This project is licensed under the MIT License.
Contributions are welcome! <https://github.com/chiyadev/nhitomi>
".Trim())
                .WithColor(Color.Purple)
                .WithThumbnailUrl(_settings.ImageUrl)
                .WithCurrentTimestamp()
                .Build();

        public Embed CreateErrorEmbed() =>
            new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    "Sorry, we encountered an unexpected error and have reported it to the developers! " +
                    $"Please join our official server for further assistance: {_settings.Discord.Guild.GuildInvite}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build();

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public Embed CreateDownloadEmbed(Doujin doujin) =>
            new EmbedBuilder()
                .WithTitle($"**{doujin.Source}**: {doujin.OriginalName ?? doujin.PrettyName}")
                .WithUrl($"https://nhitomi.chiya.dev/dl/{doujin.Source}/{doujin.SourceId}")
                .WithDescription(
                    $"Click the link above to start downloading `{doujin.OriginalName ?? doujin.PrettyName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();

        public string UnsupportedSource(string source) =>
            $"**nhitomi**: Source `{source}` is not supported. " +
            $"See **{_settings.Discord.Prefix}help** for a list of supported sources.";

        public string DoujinNotFound(string source = null) =>
            $"**{source ?? "nhitomi"}**: No such doujin!";

        public string InvalidQuery(string source = null) =>
            $"**{source ?? "nhitomi"}**: Please specify your query.";

        public string JoinGuildForDownload =>
            $"**nhitomi**: Please join our server to enable downloading! {_settings.Discord.Guild.GuildInvite}";

        public string BeginningOfList =>
            "**nhitomi**: Beginning of list!";

        public string EndOfList =>
            "**nhitomi**: End of list!";

        public string EmptyList(string source = null) =>
            $"**{source ?? "nhitomi"}**: No results!";

        public Embed CreateSubscriptionListEmbed(string[] tags) =>
            new EmbedBuilder()
                .WithTitle("**nhitomi**: Subscriptions")
                .WithDescription(tags == null || tags.Length == 0
                    ? "You have no subscriptions."
                    : $"- {string.Join("\n- ", tags)}")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp()
                .Build();

        public string AddedSubscription(string tag) =>
            $"**nhitomi**: Added tag subscription `{tag}`.";

        public string AlreadySubscribed(string tag) =>
            $"**nhitomi**: You are already subscribed to the tag `{tag}`.";

        public string RemovedSubscription(string tag) =>
            $"**nhitomi**: Removed tag subscription `{tag}`.";

        public string NotSubscribed(string tag) =>
            $"**nhitomi**: You are not subscribed to the tag `{tag}`.";

        public string ClearedSubscriptions =>
            "**nhitomi**: Cleared all tag subscriptions.";

        public Embed CreateCollectionListEmbed(string[] collectionNames) =>
            new EmbedBuilder()
                .WithTitle("**nhitomi**: Collections")
                .WithDescription(collectionNames == null || collectionNames.Length == 0
                    ? "You have no collections."
                    : $"- {string.Join("\n- ", collectionNames)}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();

        public string AddedToCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: Added `{doujin.OriginalName ?? doujin.PrettyName}` to collection `{collectionName}`.";

        public string AlreadyInCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: `{doujin.OriginalName ?? doujin.PrettyName}` already exists in collection `{collectionName}`.";

        public string RemovedFromCollection(string collectionName, CollectionItemInfo item) =>
            $"**nhitomi**: Removed `{item.Name}` from collection `{collectionName}`.";

        public string NotInCollection(string collectionName, CollectionItemInfo item) =>
            $"**nhitomi**: `{item.Source}/{item.Id}` does not exist in collection `{collectionName}`.";

        public Embed CreateCollectionEmbed(string collectionName, CollectionItemInfo[] items) =>
            new EmbedBuilder()
                .WithTitle($"**nhitomi**: Collection `{collectionName}`")
                .WithDescription(items == null || items.Length == 0
                    ? "There are no doujins in this collection."
                    : $"- {string.Join("\n- ", items.Select(i => $"`{i.Source}|{i.Id}` *{i.Artist ?? i.Source}* — `{i.Name}`"))}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();

        public Task AddCollectionTriggersAsync(IUserMessage message) =>
            message.AddReactionAsync(TrashcanEmote);

        public string CollectionDeleted(string collectionName) =>
            $"**nhitomi**: Deleted collection `{collectionName}`.";

        public string CollectionNotFound =>
            $"**nhitomi**: No such collection!";

        public string InvalidSortAttribute(string attribute) =>
            $"**nhitomi**: Attribute `{attribute}` is invalid. " +
            $"See **{_settings.Discord.Prefix}help** for a list of valid sort attributes.";

        public string SortAttributeUpdated(CollectionSortAttribute attribute) =>
            $"**nhitomi**: Updated collection sorting attribute to `{attribute}`.";
    }
}