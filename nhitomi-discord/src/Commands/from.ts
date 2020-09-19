import { CommandFunc } from ".";
import { Api } from "../api";
import {
  BookQuery,
  BookSort,
  ScraperCategory,
  SortDirection,
} from "nhitomi-api";
import { BookSearchMessage } from "./search";
import { MessageContext } from "../context";
import { Message } from "discord.js-light";

export function sourceInvalid(
  context: MessageContext,
  input: string
): Promise<Message> {
  return context.reply(
    `
${input && context.locale.get("from.badSource.message", { input })}

${context.locale.get("from.badSource.list")}
${Api.currentInfo.scrapers
  .map((s) => `> - ${s.name} — <${s.url}>`)
  .sort()
  .join("\n")}
`.trim()
  );
}

export const run: CommandFunc = async (context, source) => {
  const scraper = Api.currentInfo.scrapers.find(
    (s) => source && s.type.toLowerCase().startsWith(source.toLowerCase())
  );

  switch (scraper?.category) {
    case ScraperCategory.Book: {
      const baseQuery: BookQuery = {
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

      return new BookSearchMessage(baseQuery).initialize(context);
    }

    default: {
      await sourceInvalid(context, source || "");
      return true;
    }
  }
};
