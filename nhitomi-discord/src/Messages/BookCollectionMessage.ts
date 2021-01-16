import { BookListMessage } from "./BookListMessage";
import { Book, Collection } from "nhitomi-api";
import { AsyncArray } from "../asyncArray";
import config from "config";
import { RenderResult } from "../Interactive/message";
import { MessageContext } from "../context";
import { getWebLink } from "../client";

export class BookCollectionMessage extends BookListMessage {
  constructor(context: MessageContext, readonly collection: Collection, initial: Book) {
    super(
      context,
      new AsyncArray<Book>(config.get("search.chunkSize"), async (offset, limit) => {
        const ids = collection.items.slice(offset, offset + limit);

        if (!ids.length) {
          return [];
        }

        const results = await this.context.client.book.getBooks({
          getBookManyRequest: { ids },
        });

        return results.filter((b) => b);
      }),
      initial
    );
  }

  protected processRenderResult(result: RenderResult): RenderResult {
    result = super.processRenderResult(result);

    return {
      ...result,
      message: `
> ${this.collection.name} — ${getWebLink(`collections/${this.collection.id}`)}

${result.message || ""}
`.trim(),
    };
  }
}
