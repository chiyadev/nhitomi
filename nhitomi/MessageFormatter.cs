// Copyright (c) 2018-2019 fate/loli
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

            if (doujin.Language != null)
                embed.AddFieldSafe("Language", doujin.Language, true);
            if (doujin.ParodyOf != null)
                embed.AddFieldSafe("Parody of", doujin.ParodyOf, true);
            if (doujin.Categories != null)
                embed.AddFieldSafe("Categories", Join(doujin.Categories), true);
            if (doujin.Characters != null)
                embed.AddFieldSafe("Characters", Join(doujin.Characters), true);
            if (doujin.Tags != null)
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

        public IEnumerable<CommandInfo> AvailableCommands { get; set; }

        public Embed CreateHelpEmbed()
        {
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithDescription(
                    "nhitomi — a Discord bot for searching and downloading doujinshi by fate/loli.\n" +
                    $"Join our server: {_settings.Discord.Guild.GuildInvite}")
                .WithColor(Color.Purple)
                .WithCurrentTimestamp();

            // Commands
            var builder = new StringBuilder();

            foreach (var command in AvailableCommands)
            {
                builder.Append($"- **{_settings.Discord.Prefix}{command.Name}**");

                if (command.Parameters.Count > 0)
                    builder.Append($" __{string.Join("__ __", command.Parameters.Select(p => p.Name))}__");

                builder.AppendLine($" — {command.Summary}");
            }

            embed.AddField("— Commands —", builder);
            builder.Clear();

            // Sources
            foreach (var client in _clients)
            {
                builder.Append($"- {client.Name.ToLowerInvariant()} — {client.Url}");
                builder.AppendLine();
            }

            embed.AddField("— Sources —", builder);
            builder.Clear();

            return embed.Build();
        }

        public Embed CreateErrorEmbed() => new EmbedBuilder()
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
                .WithTitle($"**nhitomi**: {doujin.OriginalName ?? doujin.PrettyName}")
                .WithUrl($"{_settings.Http.Url}/download/{HttpUtility.UrlEncode(downloadToken)}")
                .WithDescription(
                    $"Click the link above to start downloading `{doujin.OriginalName ?? doujin.PrettyName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();
        }

        public Task AddDownloadTriggersAsync(IUserMessage message) =>
            message.AddReactionAsync(TrashcanEmote);

        public string UnsupportedSource(string source) =>
            $"**nhitomi**: Source __{source}__ is not supported. " +
            $"Please see refer to the manual (**{_settings.Discord.Prefix}help**) for a full list of supported sources.";

        public string LoadingDoujin(string source = null, string id = null) =>
            string.IsNullOrEmpty(source)
                ? "**nhitomi**: Loading..."
                : string.IsNullOrEmpty(id)
                    ? $"**{source}**: Loading..."
                    : $"**{source}**: Loading __{id}__...";

        public string DoujinNotFound(string source = null) =>
            $"**{source ?? "nhitomi"}**: No such doujin!";

        public string InvalidQuery(string source = null) =>
            $"**{source ?? "nhitomi"}**: Please specify your query.";

        public string SearchingDoujins(string source = null, string query = null) =>
            $"**{source ?? "nhitomi"}**: Searching{(query == null ? "" : $" __{query}__")}...";

        public string JoinGuildForDownload() =>
            $"**nhitomi**: Please join our server to enable downloading! {_settings.Discord.Guild.GuildInvite}";

        public string BeginningOfList() =>
            "**nhitomi**: Beginning of list!";

        public string EndOfList() =>
            "**nhitomi**: End of list!";

        public string EmptyList(string source = null) =>
            $"**{source ?? "nhitomi"}**: No results!";
    }
}
