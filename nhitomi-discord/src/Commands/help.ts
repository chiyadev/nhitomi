import { CommandFunc } from ".";
import { InteractiveMessage, ReactionTrigger, RenderResult } from "../interactive";
import { Locale } from "../locales";
import { ListTrigger } from "../Triggers/list";
import { Api } from "../api";
import { MessageEmbedOptions } from "discord.js-light";
import config from "config";
import { Discord } from "../shard";
import { ScraperType } from "nhitomi-api";

type Page = "doujinshi" | "collections" | "oss";
const Pages: Page[] = ["doujinshi", "collections", "oss"];

class HelpMessage extends InteractiveMessage {
  position = 0;

  get end(): number {
    return Pages.length - 1;
  }

  get page(): Page {
    return Pages[(this.position = Math.max(0, Math.min(this.end, this.position)))];
  }

  protected async render(locale: Locale): Promise<RenderResult> {
    const embed: MessageEmbedOptions = {
      title: `**nhitomi**: ${locale.get("help.title")}`,
      color: "PURPLE",
      thumbnail: {
        url: "https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png",
      },
      footer: {
        text: `b.${Api.currentInfo.version.substring(0, 7)} — ${locale.get("help.owner")}`,
      },
    };

    const invite = locale.get("help.invite", {
      serverInvite: `https://discord.gg/${config.get<string>("serverInvite")}`,
      botInvite: `https://discord.com/oauth2/authorize?client_id=${Discord.user?.id}&scope=bot&permissions=${config.get<
        string
      >("botInvitePerms")}`,
    });

    const prefix = config.get<string>("prefix");

    switch (this.page) {
      case "doujinshi":
        embed.description = invite;

        embed.fields = [
          {
            name: locale.get("help.doujinshi.title"),
            value: `
- \`${prefix}get\` — ${locale.get("help.doujinshi.get")}
- \`${prefix}from\` — ${locale.get("help.doujinshi.from")}
- \`${prefix}search\` — ${locale.get("help.doujinshi.search")}
- \`${prefix}read\` — ${locale.get("help.doujinshi.read")}
`.trim(),
          },
          {
            name: locale.get("help.doujinshi.sources.title"),
            value: Api.currentInfo.scrapers
              .filter((s) => s.type !== ScraperType.Unknown)
              .map((s) => `- ${s.name} — ${s.url}`)
              .sort()
              .join("\n"),
          },
        ];
        break;

      case "collections":
        embed.fields = [
          {
            name: locale.get("help.collections.title"),
            value: `
- \`${prefix}collection\` — ${locale.get("help.collections.list")}
- \`${prefix}collection {name}\` — ${locale.get("help.collections.show")}
- \`${prefix}collection {name} add\` — ${locale.get("help.collections.add")}
- \`${prefix}collection {name} remove\` — ${locale.get("help.collections.remove")}
- \`${prefix}collection {name} delete\` — ${locale.get("help.collections.delete")}
`.trim(),
          },
        ];
        break;

      case "oss":
        embed.fields = [
          {
            name: locale.get("help.oss.title"),
            value: `
${locale.get("help.oss.license")}
[GitHub](https://github.com/chiyadev/nhitomi) / [License](https://github.com/chiyadev/nhitomi/blob/master/LICENSE)
`.trim(),
          },
        ];
        break;
    }

    return { embed };
  }

  protected createTriggers(): ReactionTrigger[] {
    return [...super.createTriggers(), new ListTrigger(this, "left"), new ListTrigger(this, "right")];
  }
}

export const run: CommandFunc = (context) => new HelpMessage().initialize(context);
