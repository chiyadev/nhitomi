import { AsyncArray } from "../asyncArray";
import { Book, ObjectType, SpecialCollection } from "nhitomi-api";
import { BookMessage } from "./BookMessage";
import { ReadTrigger } from "../Triggers/ReadTrigger";
import { SourcesTrigger } from "../Triggers/SourcesTrigger";
import { ListTrigger } from "../Triggers/ListTrigger";
import { DestroyTrigger } from "../Triggers/DestroyTrigger";
import { InteractiveMessage, RenderResult } from "../Interactive/message";
import { ReactionTrigger } from "../Interactive/trigger";
import { FavoriteTrigger } from "../Triggers/FavoriteTrigger";
import { MessageContext } from "../context";

export class BookListMessage extends InteractiveMessage {
  position = 0;
  book: Book;

  get content() {
    return (
      this.book.contents.find((content) => content.language === this.context.user.language) || this.book.contents[0]
    );
  }

  get favoriteObject() {
    return {
      id: this.book.id,
      name: this.book.primaryName,
    };
  }

  constructor(context: MessageContext, readonly items: AsyncArray<Book>, initial: Book) {
    super(context);

    this.book = initial;
  }

  protected async render() {
    const current = await this.items.get(this.position);

    if (current) {
      this.book = current;
    } else {
      this.position = Math.max(0, Math.min(this.items.cachedLength - 1, this.position));
    }

    return this.processRenderResult(BookMessage.renderStatic(this.context.locale, this.book, this.content));
  }

  protected processRenderResult(result: RenderResult): RenderResult {
    return result;
  }

  protected createTriggers(): ReactionTrigger[] {
    if (!this.book) return [];

    return [
      ...super.createTriggers(),

      new FavoriteTrigger(this, ObjectType.Book, SpecialCollection.Favorites),
      new ReadTrigger(this),
      new SourcesTrigger(this),
      new ListTrigger(this, "left"),
      new ListTrigger(this, "right"),
      new DestroyTrigger(this),
    ];
  }
}
