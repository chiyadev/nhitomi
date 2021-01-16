import { InteractiveMessage } from "../Interactive/message";
import { MessageContext } from "../context";
import { Book, BookContent } from "nhitomi-api";
import { CurrentInfo, getApiLink, getWebLink } from "../client";
import { ReactionTrigger } from "../Interactive/trigger";
import { DestroyTrigger } from "../Triggers/DestroyTrigger";

export class BookSourcesMessage extends InteractiveMessage {
  constructor(context: MessageContext, readonly book: Book, readonly content: BookContent) {
    super(context);
  }

  protected async render() {
    return {
      embed: {
        title: this.book.primaryName,
        url: getWebLink(`books/${this.book.id}/contents/${this.content.id}`),
        thumbnail: {
          url: getApiLink(`books/${this.book.id}/contents/${this.content.id}/pages/-1`),
        },
        color: "GREEN",
        author: {
          name: (this.book.tags.artist || this.book.tags.circle || [this.content.source]).sort().join(", "),
          iconURL: getWebLink(`assets/icons/${this.content.source}.jpg`),
        },
        footer: {
          text: this.context.locale.get("get.sources.footer", {
            count: this.book.contents.length,
          }),
        },
        fields: Object.entries(
          this.book.contents.reduce((groups, content) => {
            const language = `${CurrentInfo?.scrapers.find((s) => s.type === content.source)?.name} (${
              content.language.split("-")[0]
            })`;

            (groups[language] = groups[language] || []).push(content);
            return groups;
          }, {} as Record<string, BookContent[]>)
        )
          .map(([source, contents]) => ({
            name: source,
            value: contents
              .map((content) => {
                let url = content.sourceUrl;

                if (content === this.content) {
                  url = `**${url}**`;
                }

                return url;
              })
              .join("\n"),
          }))
          .sort((a, b) => a.name.localeCompare(b.name)),
      },
    };
  }

  protected createTriggers(): ReactionTrigger[] {
    return [...super.createTriggers(), new DestroyTrigger(this)];
  }
}
