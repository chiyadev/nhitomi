import { Book, BookContent } from "nhitomi-api";
import { InteractiveMessage } from "../Interactive/message";
import { ReactionTrigger } from "../Interactive/trigger";
import { BookSourcesMessage } from "../Messages/BookSourcesMessage";

export type SourcesTriggerTarget = {
  book: Book;
  content: BookContent;
};

export class SourcesTrigger extends ReactionTrigger {
  constructor(readonly interactive: InteractiveMessage & SourcesTriggerTarget) {
    super(interactive, "\ud83d\udd17");
  }

  protected run() {
    return new BookSourcesMessage(this.context, this.interactive.book, this.interactive.content).update();
  }
}
