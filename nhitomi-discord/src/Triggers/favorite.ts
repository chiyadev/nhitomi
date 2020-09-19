import { ReactionTrigger } from "../interactive";
import {
  CollectionInsertPosition,
  ObjectType,
  SpecialCollection,
} from "nhitomi-api";
import { MessageContext } from "../context";

export type FavoriteTriggerTarget = {
  favoriteObject?: {
    id: string;
    name: string;
  };
};

export class FavoriteTrigger extends ReactionTrigger {
  readonly emoji = "\u2764";

  constructor(
    readonly target: FavoriteTriggerTarget,
    readonly type: ObjectType,
    readonly collection: SpecialCollection
  ) {
    super();
  }

  protected async run(context: MessageContext): Promise<boolean> {
    if (!this.target.favoriteObject) return false;

    const itemId = this.target.favoriteObject.id;
    const itemName = this.target.favoriteObject.name;

    const {
      id: collectionId,
      items,
    } = await context.api.user.getUserSpecialCollection({
      id: context.user.id,
      type: this.type,
      collection: this.collection,
    });

    if (items.includes(itemId)) {
      await context.api.collection.removeCollectionItems({
        id: collectionId,
        collectionItemsRequest: {
          items: [itemId],
        },
      });

      context.scheduleDelete(
        await context.reply(
          context.locale.get("reaction.favorite.remove", { name: itemName })
        )
      );
    } else {
      await context.api.collection.addCollectionItems({
        id: collectionId,
        addCollectionItemsRequest: {
          items: [itemId],
          position: CollectionInsertPosition.Start,
        },
      });

      context.scheduleDelete(
        await context.reply(
          context.locale.get("reaction.favorite.add", { name: itemName })
        )
      );
    }

    return true;
  }
}
