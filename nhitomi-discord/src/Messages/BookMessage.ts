import { Book, BookContent, BookTag, ObjectType, SpecialCollection } from "nhitomi-api";
import { Locale } from "../locales";
import { FavoriteTrigger } from "../Triggers/FavoriteTrigger";
import { ReadTrigger } from "../Triggers/ReadTrigger";
import { SourcesTrigger } from "../Triggers/SourcesTrigger";
import { DestroyTrigger } from "../Triggers/DestroyTrigger";
import { InteractiveMessage, RenderResult } from "../Interactive/message";
import { MessageContext } from "../context";
import { getApiLink, getWebLink } from "../client";
import { ReactionTrigger } from "../Interactive/trigger";

export class BookMessage extends InteractiveMessage {
  get favoriteObject() {
    return {
      id: this.book.id,
      name: this.book.primaryName,
    };
  }

  constructor(context: MessageContext, readonly book: Book, public content: BookContent) {
    super(context);
  }

  protected async render() {
    return BookMessage.renderStatic(this.context.locale, this.book, this.content);
  }

  static renderStatic(locale: Locale, book: Book, content: BookContent): RenderResult {
    return {
      embed: {
        title: book.primaryName,
        description: book.englishName === book.primaryName ? undefined : book.englishName,
        url: getWebLink(`books/${book.id}/contents/${content.id}`),
        image: {
          url: getApiLink(`books/${book.id}/contents/${content.id}/pages/-1`),
        },
        color: "GREEN",
        author: {
          name: (book.tags.artist || book.tags.circle || [content.source]).sort().join(", "),
          iconURL: getWebLink(`assets/icons/${content.source}.jpg`),
        },
        footer: {
          text: `${book.id}/${content.id} (${locale.get(`get.book.categories.${book.category}`)}, ${locale.get(
            "get.book.pageCount",
            {
              count: content.pageCount,
            }
          )})`,
        },
        fields: Object.values(BookTag)
          .filter((t) => book.tags[t]?.length)
          .map((t) => ({
            name: locale.get(`get.book.tags.${t}`),
            value: book.tags[t]?.sort().join(", "),
            inline: true,
          })),
      },
    };
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new FavoriteTrigger(this, ObjectType.Book, SpecialCollection.Favorites),
      new ReadTrigger(this),
      new SourcesTrigger(this),
      new DestroyTrigger(this),
    ];
  }
}
