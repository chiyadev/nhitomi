// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using nhitomi.Core;
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
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _json;

        public MessageFormatter(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer json)
        {
            _settings = options.Value;
            _clients = clients;
            _json = json;
        }

        static string Join(IEnumerable<string> values)
        {
            var array = values?.ToArray();

            return array == null || array.Length == 0
                ? null
                : string.Join(", ", array);
        }

        public Embed CreateDoujinEmbed(IDoujin doujin)
        {
            var embed = new EmbedBuilder()
                .WithTitle(doujin.OriginalName ?? doujin.PrettyName)
                .WithDescription(doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName)
                .WithAuthor(a => a
                    .WithName(Join(doujin.Artists) ?? doujin.Source.Name)
                    .WithIconUrl(doujin.Source.IconUrl))
                .WithUrl(doujin.SourceUrl)
                .WithImageUrl(doujin.Pages.First().Url)
                .WithColor(Color.Green)
                .WithFooter($"{doujin.Source.Name}/{doujin.Id}");

            embed.AddFieldSafe("Language", doujin.Language, true);
            embed.AddFieldSafe("Parody of", doujin.ParodyOf, true);
            embed.AddFieldSafe("Categories", Join(doujin.Categories), true);
            embed.AddFieldSafe("Characters", Join(doujin.Characters), true);
            embed.AddFieldSafe("Tags", Join(doujin.Tags), true);

            embed.AddField("Content", $"{doujin.PageCount} pages", true);

            return embed.Build();
        }

        public Task AddDoujinTriggersAsync(IUserMessage message) =>
            message.AddReactionsAsync(new[]
            {
                FloppyDiskEmote,
                TrashcanEmote
            });

        public Task AddFeedDoujinTriggersAsync(IUserMessage message) =>
            message.AddReactionAsync(HeartEmote);

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
                    "nhitomi — a Discord bot for searching and downloading doujinshi, by __chiya.dev__.\n" +
                    $"Join our server: {_settings.Discord.Guild.GuildInvite}")
                .AddField("  — Commands —", $@"
- {_settings.Discord.Prefix}get __source__ __id__ — Retrieves doujin information from a source by its ID.
- {_settings.Discord.Prefix}all __source__ — Displays all doujins from a source uploaded recently.
- {_settings.Discord.Prefix}search __query__ — Searches for doujins by the title and tags that satisfy your query.
- {_settings.Discord.Prefix}download __source__ __id__ — Sends a download link for a doujin by its ID.

- {_settings.Discord.Prefix}subs — Lists all tags you are subscribed to.
- {_settings.Discord.Prefix}subs add|remove __tag__ — Adds or removes a tag subscription.
- {_settings.Discord.Prefix}subs enable|disable — Enables or disables subscription notifications.
- {_settings.Discord.Prefix}subs clear — Removes all tag subscriptions.

- {_settings.Discord.Prefix}collection __name__ — Displays doujins belonging to a collection.
- {_settings.Discord.Prefix}collection __name__ add __source__ __id__ — Adds a doujin to a collection.
- {_settings.Discord.Prefix}collection __name__ remove __source__ __id__ — Removes a doujin from a collection.
- {_settings.Discord.Prefix}collection list __name__ — Shows all doujins belonging to a collection in the form of a list.
- {_settings.Discord.Prefix}collection order __name__ __indices__ — Reorders doujins in a collection by their indices.
- {_settings.Discord.Prefix}collection sort __name__ __attribute__ — Reorders doujins in a collection by their attribute.
- {_settings.Discord.Prefix}collection delete __name__ — Deletes a collection, removing all doujins belonging to it.

- {_settings.Discord.Prefix}help — Shows this help message.
".Trim())
                .AddField("  — Sources —", @"
- nhentai — `https://nhentai.net/`
- hitomi — `https://hitomi.la/`
~~- tsumino — `https://tsumino.com/`~~
~~- pururin — `https://pururin.io/`~~
".Trim())
                .WithColor(Color.Purple)
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

        public Embed CreateDownloadEmbed(IDoujin doujin)
        {
            var downloadToken = TokenGenerator.CreateToken(
                new TokenGenerator.ProxyDownloadPayload
                {
                    Source = doujin.Source.Name,
                    Id = doujin.Id,
                    RequestThrottle = doujin.Source.RequestThrottle,
                    Expires = TokenGenerator.GetExpirationFromNow(_settings.Doujin.DownloadValidLength)
                },
                _settings.Discord.Token,
                serializer: _json);

            return new EmbedBuilder()
                .WithTitle($"**{doujin.Source.Name}**: {doujin.OriginalName ?? doujin.PrettyName}")
                .WithUrl($"{_settings.Http.Url}/download?token={HttpUtility.UrlEncode(downloadToken)}")
                .WithDescription(
                    $"Click the link above to start downloading `{doujin.OriginalName ?? doujin.PrettyName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();
        }

        public string UnsupportedSource(string source) =>
            $"**nhitomi**: Source __{source}__ is not supported. " +
            $"Please see refer to the manual (**{_settings.Discord.Prefix}help**) for a full list of supported sources.";

        public string DoujinNotFound(string source = null) =>
            $"**{source ?? "nhitomi"}**: No such doujin!";

        public string InvalidQuery(string source = null) =>
            $"**{source ?? "nhitomi"}**: Please specify your query.";

        public string JoinGuildForDownload() =>
            $"**nhitomi**: Please join our server to enable downloading! {_settings.Discord.Guild.GuildInvite}";

        public string BeginningOfList() =>
            "**nhitomi**: Beginning of list!";

        public string EndOfList() =>
            "**nhitomi**: End of list!";

        public string EmptyList(string source = null) =>
            $"**{source ?? "nhitomi"}**: No results!";

        public Embed CreateSubscriptionListEmbed(IEnumerable<string> tags) =>
            new EmbedBuilder()
                .WithTitle($"**nhitomi**: Subscriptions")
                .WithDescription(tags.Any()
                    ? $"- {string.Join("\n- ", tags)}"
                    : "You have no subscriptions.")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();

        public string SubscribeSuccess(string tag) =>
            $"**nhitomi**: Subscribed to tag '{tag}'.";

        public string UnsubscribeSuccess(string tag) =>
            $"**nhitomi**: Unsubscribed from tag '{tag}'.";

        public string SubscriptionsClearSuccess() =>
            "**nhitomi**: Cleared all tag subscriptions.";
    }
}
