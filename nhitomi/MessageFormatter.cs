// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Linq;
using Discord;
using nhitomi.Core;

namespace nhitomi
{
    public class MessageFormatter
    {
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

        public string AddedToCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: Added `{doujin.OriginalName ?? doujin.PrettyName}` to collection `{collectionName}`.";

        public string AlreadyInCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: `{doujin.OriginalName ?? doujin.PrettyName}` already exists in collection `{collectionName}`.";

        public string RemovedFromCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: Removed `{doujin.Name}` from collection `{collectionName}`.";

        public string NotInCollection(string collectionName, Doujin doujin) =>
            $"**nhitomi**: `{doujin.Source}/{doujin.Id}` does not exist in collection `{collectionName}`.";

        public Embed CreateCollectionEmbed(string collectionName, Doujin[] doujin) =>
            new EmbedBuilder()
                .WithTitle($"**nhitomi**: Collection `{collectionName}`")
                .WithDescription(doujin == null || doujin.Length == 0
                    ? "There are no doujins in this collection."
                    : $"- {string.Join("\n- ", doujin.Select(i => $"`{i.Source}|{i.Id}` *{i.Artist ?? i.Source}* — `{i.Name}`"))}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();

        public string CollectionDeleted(string collectionName) =>
            $"**nhitomi**: Deleted collection `{collectionName}`.";

        public string CollectionNotFound =>
            $"**nhitomi**: No such collection!";

        public string InvalidSortAttribute(string attribute) =>
            $"**nhitomi**: Attribute `{attribute}` is invalid. " +
            $"See **{_settings.Discord.Prefix}help** for a list of valid sort attributes.";

        public string SortAttributeUpdated(CollectionSort attribute) =>
            $"**nhitomi**: Updated collection sorting attribute to `{attribute}`.";
    }
}