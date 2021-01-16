import { Book, BookContent, ObjectType } from "nhitomi-api";
import { MessageContext } from "../context";
import { CommandFunc } from "../Shard/command";
import { BookMessage } from "../Messages/BookMessage";

export const run: CommandFunc = async (context, link) => {
  const result = await handleGetLink(context, link);

  switch (result.type) {
    case ObjectType.Book:
      return await new BookMessage(context, result.book, result.content).update();

    case "notFound":
      await replyNotFound(context, link || "");
      return true;
  }
};

export async function handleGetLink(
  context: MessageContext,
  link: string | undefined
): Promise<
  | {
      type: ObjectType.Book;
      book: Book;
      content: BookContent;
    }
  | {
      type: "notFound";
    }
> {
  if (link) {
    // try finding books
    const {
      matches: [bookMatch],
    } = await context.client.book.getBooksByLink({
      strict: true,
      getBookByLinkRequest: { link },
    });

    if (bookMatch) {
      const { book, selectedContentId } = bookMatch;

      return {
        type: ObjectType.Book,
        book,
        content: book.contents.find((c) => c.id === selectedContentId) || book.contents[0],
      };
    }
  }

  return { type: "notFound" };
}

export function replyNotFound(context: MessageContext, input: string) {
  return context.reply(
    `
${input && context.locale.get("get.notFound.message", { input })}

> - ${context.locale.get("get.notFound.usageLink", {
      example: "https://nhentai.net/g/123/",
    })}
> - ${context.locale.get("get.notFound.usageSource", {
      example: "hitomi 123",
    })}
`.trim()
  );
}
