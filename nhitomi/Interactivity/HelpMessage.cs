using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public enum HelpMessageSection
    {
        Doujins,
        Collections,
        Options,
        Other
    }

    public class HelpMessage : ListMessage<HelpMessage.View, HelpMessageSection>
    {
        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new ListTrigger(MoveDirection.Left);
            yield return new ListTrigger(MoveDirection.Right);
            yield return new DeleteTrigger();
        }

        public class View : SynchronousListViewBase
        {
            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override HelpMessageSection[] GetValues(int offset) => Enum.GetValues(typeof(HelpMessageSection))
                                                                                 .Cast<HelpMessageSection>()
                                                                                 .Skip(offset)
                                                                                 .ToArray();

            protected override Embed CreateEmbed(HelpMessageSection value)
            {
                var l = Context.GetLocalization()["helpMessage"];

                var embed = new EmbedBuilder
                {
                    Title        = $"**nhitomi**: {l["title"]}",
                    Color        = Color.Purple,
                    ThumbnailUrl = "https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"v{VersionHelper.Version.ToString(2)} {VersionHelper.Codename} — {l["footer"]}"
                    }
                };

                switch (value)
                {
                    case HelpMessageSection.Doujins:

                        embed.Description =
                            $"nhitomi — {l["about"]}\n\n" +
                            $"{l["invite", new { botInvite = _settings.Discord.BotInvite, guildInvite = _settings.Discord.Guild.GuildInvite }]}";

                        DoujinsSection(embed, l);
                        SourcesSection(embed, l);
                        break;

                    case HelpMessageSection.Collections:
                        CollectionsSection(embed, l);
                        break;

                    case HelpMessageSection.Options:
                        OptionsSection(embed, l);
                        LanguagesSection(embed, l);
                        break;

                    case HelpMessageSection.Other:
                        // only add translators if not English
                        if (l.Localization != Localization.Default)
                            TranslationsSection(embed, l);

                        OpenSourceSection(embed, l);
                        break;
                }

                return embed.Build();
            }

            void DoujinsSection(EmbedBuilder embed,
                                LocalizationAccess l)
            {
                l = l["doujins"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField(l["title"],
                               $@"
- `{prefix}get` — {l["get"]}
- `{prefix}from` — {l["from"]}
- `{prefix}read` — {l["read"]}
- `{prefix}download` — {l["download"]}
- `{prefix}search` — {l["search"]}
".Trim());
            }

            static void SourcesSection(EmbedBuilder embed,
                                       LocalizationAccess l)
            {
                l = l["doujins"]["sources"];

                embed.AddField(l["title"],
                               @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim());
            }

            void CollectionsSection(EmbedBuilder embed,
                                    LocalizationAccess l)
            {
                l = l["collections"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField(l["title"],
                               $@"
- `{prefix}collections` — {l["list"]}
- `{prefix}collection {{name}}` — {l["view"]}
- `{prefix}collection {{name}} add` — {l["add"]}
- `{prefix}collection {{name}} remove` — {l["remove"]}
- `{prefix}collection {{name}} sort` — {l["sort"]}
- `{prefix}collection {{name}} delete` — {l["delete"]}
".Trim());
            }

            void OptionsSection(EmbedBuilder embed,
                                LocalizationAccess l)
            {
                l = l["options"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField(l["title"],
                               $@"
- `{prefix}option language` — {l["language"]}
- `{prefix}option feed add` — {l["feed.add"]}
- `{prefix}option feed remove` — {l["feed.remove"]}
- `{prefix}option feed mode` — {l["feed.mode"]}
".Trim());
            }

            static void LanguagesSection(EmbedBuilder embed,
                                         LocalizationAccess l)
            {
                l = l["options"]["languages"];

                var content = new StringBuilder();

                foreach (var localization in Localization.GetAllLocalizations())
                {
                    var culture = localization.Culture;

                    content.AppendLine($"- `{culture.Name}` — {culture.EnglishName} | {culture.NativeName}");
                }

                embed.AddField(l["title"], content.ToString());
            }

            static void TranslationsSection(EmbedBuilder embed,
                                            LocalizationAccess l)
            {
                l = l["translations"];

                embed.AddField(
                    l["title"],
                    l["text", new { translators = l.Localization["meta"]["translators"] }]);
            }

            static void OpenSourceSection(EmbedBuilder embed,
                                          LocalizationAccess l)
            {
                l = l["openSource"];

                embed.AddField(l["title"],
                               $@"
{l["license"]}
[GitHub](https://github.com/chiyadev/nhitomi)
".Trim());
            }

            protected override Embed CreateEmptyEmbed() => throw new NotSupportedException();

            protected override string ListBeginningMessage => "helpMessage.listBeginning";
            protected override string ListEndMessage => "helpMessage.listEnd";
        }
    }
}