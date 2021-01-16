import { Book, BookQuery, BookSort, ScraperCategory, SortDirection } from "nhitomi-api";
import { MessageContext } from "../context";
import { CommandFunc } from "../Shard/command";
import { CurrentInfo } from "../client";
import { AsyncArray } from "../asyncArray";
import config from "config";
import { BookListMessage } from "../Messages/BookListMessage";
import { EmptyListMessage } from "../Messages/EmptyListMessage";

export const run: CommandFunc = async (context, source) => {
  const scraper = CurrentInfo?.scrapers.find((s) => source && s.type.toLowerCase().startsWith(source.toLowerCase()));

  switch (scraper?.category) {
    case ScraperCategory.Book: {
      const bookQuery: BookQuery = {
        source: {
          values: [scraper.type],
        },
        limit: 0,
        sorting: [
          {
            value: BookSort.UpdatedTime,
            direction: SortDirection.Descending,
          },
        ],
      };

      const items = new AsyncArray<Book>(config.get("search.chunkSize"), async (offset, limit) => {
        const { items } = await context.client.book.searchBooks({
          bookQuery: {
            ...bookQuery,
            offset,
            limit,
          },
        });

        return items;
      });

      const initial = await items.get(0);

      if (initial) {
        return await new BookListMessage(context, items, initial).update();
      } else {
        return await new EmptyListMessage(context).update();
      }
    }

    default:
      await replySourceInvalid(context, source || "");
      return true;
  }
};

export function replySourceInvalid(context: MessageContext, input: string) {
  return context.reply(
    `
${input && context.locale.get("from.badSource.message", { input })}

${context.locale.get("from.badSource.list")}
${CurrentInfo?.scrapers
  .map(({ name, url }) => `> - ${name} — <${url}>`)
  .sort()
  .join("\n")}
`.trim()
  );
}
