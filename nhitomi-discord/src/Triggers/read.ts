import { ReactionTrigger } from "../interactive";
import { Book, BookContent } from "nhitomi-api";
import { BookReadMessage } from "../Commands/read";
import { MessageContext } from "../context";

export type ReadTriggerTarget = {
  book?: Book;
  content?: BookContent;
};

export class ReadTrigger extends ReactionTrigger {
  readonly emoji = "\uD83D\uDCD6";

  constructor(readonly target: ReadTriggerTarget) {
    super();
  }

  protected async run(context: MessageContext): Promise<boolean> {
    const book = this.target.book;
    const content = this.target.content;

    if (book && content)
      return await new BookReadMessage(book, content).initialize(context);

    return false;
  }
}
