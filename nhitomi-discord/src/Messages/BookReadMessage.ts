import { Book, BookContent } from "nhitomi-api";
import { ListTrigger } from "../Triggers/ListTrigger";
import { DestroyTrigger } from "../Triggers/DestroyTrigger";
import { ReactionTrigger } from "../Interactive/trigger";
import { InteractiveMessage } from "../Interactive/message";
import { MessageContext } from "../context";
import { getApiLink, getWebLink } from "../client";
import { ListJumpTrigger } from "../Triggers/ListJumpTrigger";

export class BookReadMessage extends InteractiveMessage {
  constructor(context: MessageContext, readonly book: Book, readonly content: BookContent) {
    super(context);
  }

  position = 0;

  get length() {
    return this.content.pageCount;
  }

  protected async render() {
    this.position = Math.max(0, Math.min(this.length - 1, this.position));

    return {
      message: this.content.sourceUrl,
      embed: {
        title: this.book.primaryName,
        description: this.context.locale.get("read.pagination", {
          current: this.position + 1,
          total: this.length,
        }),
        url: getWebLink(`books/${this.book.id}/contents/${this.content.id}`),
        image: {
          url: getApiLink(`books/${this.book.id}/contents/${this.content.id}/pages/${this.position}`),
        },
        color: "DARK_GREEN",
        author: {
          name: (this.book.tags.artist || this.book.tags.circle || [this.content.source]).sort().join(", "),
          iconURL: getWebLink(`assets/icons/${this.content.source}.jpg`),
        },
        footer: {
          text: `${this.book.id}/${this.content.id}`,
        },
      },
    };
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new ListTrigger(this, "left"),
      new ListTrigger(this, "right"),
      new ListJumpTrigger(this, "input"),
      new DestroyTrigger(this),
    ];
  }
}
