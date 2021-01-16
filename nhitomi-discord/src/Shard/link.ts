import { MessageContext } from "../context";
import { AsyncArray } from "../asyncArray";
import { Message } from "discord.js-light";
import { createApiClient, CurrentInfo } from "../client";
import { BookMessage } from "../Messages/BookMessage";
import { BookListMessage } from "../Messages/BookListMessage";
import { EmptyListMessage } from "../Messages/EmptyListMessage";

const regexCache = new Map<string, RegExp>();

function buildRegex(pattern: string) {
  let regex = regexCache.get(pattern);

  if (!regex) {
    regexCache.set(pattern, (regex = new RegExp(pattern, "gi")));
  }

  return regex;
}

function isGalleryMatch(str: string) {
  if (!CurrentInfo) {
    return false;
  }

  for (const { galleryRegexLax } of CurrentInfo.scrapers) {
    if (galleryRegexLax && str.match(buildRegex(galleryRegexLax))?.length) {
      return true;
    }
  }

  return false;
}

export async function handleLinks(message: Message) {
  const content = message.content.trim();

  // optimize api calls by first checking for the existence of links
  if (!isGalleryMatch(content)) {
    return false;
  }

  const client = createApiClient();
  const result = await client.book.getBooksByLink({
    strict: false,
    getBookByLinkRequest: { link: content },
  });

  if (!result.matches.length) {
    return false;
  }

  await message.fetch();

  if (message.channel.type === "text") {
    await message.channel.fetch();
  }

  const context = await MessageContext.create(message);

  try {
    // ensure nsfw channel
    if (message.channel.type === "text" && !message.channel.nsfw) {
      await context.reply(context.locale.get("nsfw.notAllowed"));
    } else {
      switch (result.matches.length) {
        case 0:
          await new EmptyListMessage(context).update();
          break;

        case 1:
          const { book, selectedContentId } = result.matches[0];
          const content = book.contents.find((c) => c.id === selectedContentId) || book.contents[0];

          await new BookMessage(context, book, content).update();
          break;

        default:
          const items = AsyncArray.fromArray(result.matches.map((m) => m.book));
          const initial = result.matches[0].book;

          await new BookListMessage(context, items, initial).update();
          break;
      }
    }
  } finally {
    context.destroy();
  }

  return true;
}
