using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public enum HelpMessageSection
    {
        DoujinAndCollection,
        OptionsAndAliases,
        Other
    }

    public class HelpMessage : ListMessage<HelpMessage.View, HelpMessageSection>
    {
        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        public class View : ListViewBase
        {
            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override Task<HelpMessageSection[]> GetValuesAsync(int offset,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(
                    Enum.GetValues(typeof(HelpMessageSection))
                        .Cast<HelpMessageSection>()
                        .Skip(offset)
                        .ToArray());

            protected override Embed CreateEmbed(HelpMessageSection value)
            {
                var path = new LocalizationPath("helpMessage");
                var l = Context.GetLocalization();

                var embed = new EmbedBuilder()
                    .WithTitle(path["title"][l])
                    .WithColor(Color.Purple)
                    .WithThumbnailUrl("https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png")
                    .WithFooter(path["footer"][l, new
                    {
                        version = typeof(Startup).Assembly
                            .GetName().Version
                            .ToString(2),
                        codename = typeof(Startup).Assembly
                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                            .InformationalVersion
                    }]);

                switch (value)
                {
                    case HelpMessageSection.DoujinAndCollection:
                        embed.Description =
                            $"nhitomi — {path["about"][l]}\n\n" +
                            $"{path["invite"][l, new {botInvite = _settings.Discord.BotInvite, guildInvite = _settings.Discord.Guild.GuildInvite}]}";

                        DoujinsSection(embed, path, l);
                        CollectionsSection(embed, path, l);
                        break;

                    case HelpMessageSection.OptionsAndAliases:
                        OptionsSection(embed, path, l);
                        AliasesSection(embed, path, l);
                        break;

                    case HelpMessageSection.Other:
                        SourcesSection(embed, path, l);
                        LanguagesSection(embed, path, l);

                        // only add translators if not English
                        if (l != Localization.Default)
                            TranslationsSection(embed, path, l);

                        OpenSourceSection(embed, path, l);
                        break;
                }

                return embed.Build();
            }

            void DoujinsSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["doujins"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —", $@"
- {prefix}get `source` `id` — {path["get"][l]}
- {prefix}from `source` — {path["from"][l]}
- {prefix}read `source` `id` — {path["read"][l]}
- {prefix}download `source` `id` — {path["download"][l]}
- {prefix}search `query` — {path["search"][l]}
".Trim());
            }

            void CollectionsSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["collections"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —", $@"
- {prefix}collection list — {path["list"][l]}
- {prefix}collection `name` — {path["view"][l]}
- {prefix}collection `name` add `source` `id` — {path["add"][l]}
- {prefix}collection `name` remove `source` `id` — {path["remove"][l]}
- {prefix}collection `name` sort `attribute` — {path["sort"][l]}
- {prefix}collection `name` delete — {path["delete"][l]}
".Trim());
            }

            void OptionsSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["options"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —", $@"
- {prefix}option language `name` — {path["language"][l]}
- {prefix}option filter `on or off` — {path["filter"][l]}
- {prefix}option feed add `tag` — {path["feed.add"][l]}
- {prefix}option feed remove `tag` — {path["feed.remove"][l]}
- {prefix}option feed mode `mode` — {path["feed.mode"][l]}
".Trim());
            }

            void AliasesSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["aliases"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —", $@"
{prefix}**h** — help
{prefix}**g** `source` `id` — get
{prefix}**f** `source` — from
{prefix}**s** `query` — search
{prefix}**dl** `source` `id` — download
{prefix}**c** **l** — collection list
{prefix}**c** `name` — collection
{prefix}**c** `name` **a** `source` `id` — collection add
{prefix}**c** `name` **r** `source` `id` — collection remove
{prefix}**c** `name` **s** `attribute` — collection sort
{prefix}**c** `name` **d** — collection delete
{prefix}**o** **l** `name` — option language
{prefix}**o** **f** `on or off` — option filter
{prefix}**o** **f** **a** `tag` — option feed add
{prefix}**o** **f** **r** `tag` — option feed remove
{prefix}**o** **f** **m** `mode` — option feed mode
".Trim());
            }

            static void SourcesSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["sources"];

                embed.AddField($"— {path["heading"][l]} —", @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim());
            }

            static void LanguagesSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path["languages"];

                var builder = new StringBuilder();

                foreach (var localization in Localization.GetAllLocalizations())
                    builder.AppendLine($"- `{localization.Culture.Name}` " +
                                       $"— {localization.Culture.EnglishName} | {localization.Culture.DisplayName}");

                embed.AddField($"— {path["heading"][l]} —", builder.ToString());
            }

            static void TranslationsSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path.Up["translations"];

                embed.AddField(
                    $"— {path["heading"][l]} —",
                    path["text"][l, new {translators = new LocalizationPath()["meta.translators"][l]}]);
            }

            static void OpenSourceSection(EmbedBuilder embed, LocalizationPath path, Localization l)
            {
                path = path.Up["openSource"];

                embed.AddField($"— {path["heading"][l]} —", $@"
{path["license"][l]}
{path["contribution"][l, new {repoUrl = "https://github.com/chiyadev/nhitomi"}]}
".Trim());
            }

            protected override Embed CreateEmptyEmbed() => throw new NotSupportedException();
        }
    }
}