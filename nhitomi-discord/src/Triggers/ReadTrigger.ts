import { Book, BookContent } from "nhitomi-api";
import { ReactionTrigger } from "../Interactive/trigger";
import { InteractiveMessage } from "../Interactive/message";
import { BookReadMessage } from "../Messages/BookReadMessage";

export type ReadTriggerTarget = {
  book: Book;
  content: BookContent;
};

export class ReadTrigger extends ReactionTrigger {
  constructor(readonly interactive: InteractiveMessage & ReadTriggerTarget) {
    super(interactive, "\uD83D\uDCD6");
  }

  protected run() {
    return new BookReadMessage(this.context, this.interactive.book, this.interactive.content).update();
  }
}
