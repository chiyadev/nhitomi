import { ObjectType } from "nhitomi-api";
import { handleGetLink, replyNotFound } from "./get";
import { BookReadMessage } from "../Messages/BookReadMessage";
import { CommandFunc } from "../Shard/command";

export const run: CommandFunc = async (context, link) => {
  const result = await handleGetLink(context, link);

  switch (result.type) {
    case ObjectType.Book:
      return await new BookReadMessage(context, result.book, result.content).update();

    case "notFound":
      await replyNotFound(context, link || "");
      return true;
  }
};
