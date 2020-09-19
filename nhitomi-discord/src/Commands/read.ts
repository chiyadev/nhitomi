import { CommandFunc } from ".";
import { InteractiveMessage, ReactionTrigger, RenderResult } from "../interactive";
import { Locale } from "../locales";
import { Book, BookContent, ObjectType } from "nhitomi-api";
import { handleGetLink, replyNotFound } from "./get";
import { DestroyTrigger } from "../Triggers/destroy";
import { ListJumpTrigger, ListTrigger } from "../Triggers/list";
import { Api } from "../api";

export class BookReadMessage extends InteractiveMessage {
  constructor(readonly book: Book, readonly content: BookContent) {
    super();
  }

  position = 0;

  get end(): number {
    return this.content.pageCount - 1;
  }

  protected async render(locale: Locale): Promise<RenderResult> {
    this.position = Math.max(0, Math.min(this.end, this.position));

    const book = this.book;
    const content = this.content;

    return {
      message: content.sourceUrl,
      embed: {
        title: book.primaryName,
        description: locale.get("read.pagination", {
          current: this.position + 1,
          total: this.end + 1,
        }),
        url: Api.getWebLink(`books/${book.id}/contents/${content.id}`),
        image: {
          url: Api.getApiLink(`books/${book.id}/contents/${content.id}/pages/${this.position}`),
        },
        color: "DARK_GREEN",
        author: {
          name: (book.tags.artist || book.tags.circle || [content.source]).sort().join(", "),
          iconURL: Api.getWebLink(`assets/icons/${content.source}.jpg`),
        },
        footer: {
          text: `${book.id}/${content.id}`,
        },
      },
    };
  }

  createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new ListTrigger(this, "left"),
      new ListTrigger(this, "right"),
      new ListJumpTrigger(this, "input"),
      new DestroyTrigger(),
    ];
  }
}

export const run: CommandFunc = async (context, link) => {
  const result = await handleGetLink(context, link);

  switch (result.type) {
    case ObjectType.Book: {
      const { book, content } = result;

      return await new BookReadMessage(book, content).initialize(context);
    }

    case "notFound": {
      await replyNotFound(context, link || "");
      return true;
    }
  }
};
