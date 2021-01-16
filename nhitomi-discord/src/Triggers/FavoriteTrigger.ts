import { CollectionInsertPosition, ObjectType, SpecialCollection } from "nhitomi-api";
import { ReactionTrigger } from "../Interactive/trigger";
import { InteractiveMessage } from "../Interactive/message";

export type FavoriteTriggerTarget = {
  favoriteObject: {
    id: string;
    name: string;
  };
};

export class FavoriteTrigger extends ReactionTrigger {
  constructor(
    readonly interactive: InteractiveMessage & FavoriteTriggerTarget,
    readonly type: ObjectType,
    readonly collection: SpecialCollection
  ) {
    super(interactive, "\u2764");
  }

  protected async run() {
    if (!this.interactive.favoriteObject) {
      return false;
    }

    const { id, name } = this.interactive.favoriteObject;

    const { id: collectionId, items } = await this.context.client.user.getUserSpecialCollection({
      id: this.context.user.id,
      type: this.type,
      collection: this.collection,
    });

    if (items.includes(id)) {
      await this.context.client.collection.removeCollectionItems({
        id: collectionId,
        collectionItemsRequest: {
          items: [id],
        },
      });

      await this.context.notify(this.context.locale.get("reaction.favorite.remove", { name }));
    } else {
      await this.context.client.collection.addCollectionItems({
        id: collectionId,
        addCollectionItemsRequest: {
          items: [id],
          position: CollectionInsertPosition.Start,
        },
      });

      await this.context.notify(this.context.locale.get("reaction.favorite.add", { name }));
    }

    return true;
  }
}
