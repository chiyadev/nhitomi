import { handleGetLink, replyNotFound } from "./get";
import { Collection, CollectionInsertPosition, ObjectType } from "nhitomi-api";
import { CommandFunc } from "../Shard/command";
import { MessageContext } from "../context";
import { BookCollectionMessage } from "../Messages/BookCollectionMessage";
import { CollectionListMessage } from "../Messages/CollectionListMessage";
import { AsyncArray } from "../asyncArray";
import { EmptyListMessage } from "../Messages/EmptyListMessage";

const commandRegex = /(?<collection>.+)\s+(?<command>add|remove|delete)(\s+(?<link>.+))?/ms;

export const run: CommandFunc = async (context, arg) => {
  const match = commandRegex.exec(arg || "");
  const command = match?.groups?.command?.trim().toLowerCase() || "";
  const collectionName = match?.groups?.collection?.trim() || arg || "";
  const link = match?.groups?.link?.trim() || "";

  let { items: collections } = await context.client.user.getUserCollections({
    id: context.user.id,
  });

  let collection: Collection | undefined;

  if (collectionName) {
    collections = collections.filter((c) => c.name.toLowerCase().startsWith(collectionName.toLowerCase()));
    collection = await selectCollection(context, collections);
  }

  switch (command) {
    case "add":
    case "remove": {
      const linkResult = await handleGetLink(context, link);

      if (linkResult.type === "notFound") {
        await replyNotFound(context, link);
        return true;
      }

      if (!collection) {
        collection = await context.client.collection.createCollection({
          createCollectionRequest: {
            type: linkResult.type,
            collection: {
              name: collectionName,
            },
          },
        });
      }

      let item: {
        type: ObjectType;
        id: string;
        name: string;
      };

      switch (linkResult.type) {
        case ObjectType.Book:
          item = {
            type: ObjectType.Book,
            id: linkResult.book.id,
            name: linkResult.book.primaryName,
          };
          break;
      }

      switch (command) {
        case "add":
          await handleAddItem(context, collection, item);
          break;

        case "remove":
          await handleRemoveItem(context, collection, item);
          break;
      }

      return true;
    }

    case "delete":
      if (collection) {
        await handleDelete(context, collection);
        return true;
      }

      break;
  }

  switch (collection?.type) {
    case ObjectType.Book: {
      if (collection.items.length) {
        const initial = await context.client.book.getBook({ id: collection.items[0] });
        return await new BookCollectionMessage(context, collection, initial).update();
      } else {
        return await new EmptyListMessage(context).update();
      }
    }
  }

  if (collections.length) {
    return await new CollectionListMessage(context, AsyncArray.fromArray(collections), collections[0]).update();
  } else {
    return await new EmptyListMessage(context).update();
  }
};

async function selectCollection(context: MessageContext, collections: Collection[]) {
  if (collections.length > 1) {
    const selected = await context.waitInput(
      `
${context.locale.get("collection.select.message")}

${collections
  .map((c, i) => {
    let name = `${i + 1}. \`${c.name}\``;

    if (collections.some((c2) => c !== c2 && c.name === c2.name)) {
      name = `${name} (${c.id} ${c.type})`;
    }

    return name;
  })
  .join("\n")}
`.trim()
    );

    const index = parseInt(selected || "") - 1;

    if (isNaN(index)) {
      return collections.find((c) => !selected || c.name.toLowerCase().startsWith(selected.toLowerCase()));
    } else {
      return collections[Math.max(0, Math.min(collections.length - 1, index))];
    }
  }

  return collections[0];
}

async function handleAddItem(
  context: MessageContext,
  collection: Collection,
  item: {
    type: ObjectType;
    id: string;
    name: string;
  }
) {
  if (collection.items.includes(item.id)) {
    await context.reply(
      context.locale.get("collection.add.exists", {
        item: item.name,
        collection: collection.name,
      })
    );
  } else {
    // ensure item type is the same as collection type
    if (collection.type !== item.type) {
      await context.reply(
        context.locale.get("collection.add.typeIncompatible", {
          item: item.name,
          itemType: item.type,
          collection: collection.name,
          collectionType: collection.type,
        })
      );
    } else {
      await context.client.collection.addCollectionItems({
        id: collection.id,
        addCollectionItemsRequest: {
          items: [item.id],
          position: CollectionInsertPosition.Start,
        },
      });

      await context.reply(
        context.locale.get("collection.add.success", {
          item: item.name,
          collection: collection.name,
        })
      );
    }
  }
}

async function handleRemoveItem(
  context: MessageContext,
  collection: Collection,
  item: {
    type: ObjectType;
    id: string;
    name: string;
  }
) {
  if (collection.items.includes(item.id)) {
    await context.client.collection.removeCollectionItems({
      id: collection.id,
      collectionItemsRequest: {
        items: [item.id],
      },
    });

    await context.reply(
      context.locale.get("collection.remove.success", {
        item: item.name,
        collection: collection.name,
      })
    );
  } else {
    await context.reply(
      context.locale.get("collection.remove.notExists", {
        item: item.name,
        collection: collection.name,
      })
    );
  }
}

async function handleDelete(context: MessageContext, collection: Collection) {
  const confirm = await context.waitInput(
    context.locale.get("collection.delete.confirm", {
      collection: collection.name,
    })
  );

  if (!"yes".startsWith(confirm?.trim().toLowerCase() || "no")) {
    return true;
  }

  await context.client.collection.deleteCollection({
    id: collection.id,
  });

  await context.reply(
    context.locale.get("collection.delete.success", {
      collection: collection.name,
    })
  );
}
