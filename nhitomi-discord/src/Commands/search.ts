import { Book, BookQuery, BookSort, QueryMatchMode, SortDirection } from "nhitomi-api";
import { CommandFunc } from "../Shard/command";
import { AsyncArray } from "../asyncArray";
import config from "config";
import { BookListMessage } from "../Messages/BookListMessage";
import { EmptyListMessage } from "../Messages/EmptyListMessage";

export const run: CommandFunc = async (context, query) => {
  const bookQuery: BookQuery = {
    all: !query
      ? undefined
      : {
          mode: QueryMatchMode.All,
          values: [query],
        },
    language: {
      values: [context.user.language],
    },
    limit: 50,
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
};
