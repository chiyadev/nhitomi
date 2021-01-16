import { Collection } from "nhitomi-api";
import { Locale } from "../locales";
import { ListTrigger } from "../Triggers/ListTrigger";
import { DestroyTrigger } from "../Triggers/DestroyTrigger";
import { InteractiveMessage, RenderResult } from "../Interactive/message";
import { MessageContext } from "../context";
import { AsyncArray } from "../asyncArray";
import { getWebLink } from "../client";
import { ReactionTrigger } from "../Interactive/trigger";

export class CollectionListMessage extends InteractiveMessage {
  position = 0;
  collection: Collection;

  constructor(context: MessageContext, readonly items: AsyncArray<Collection>, initial: Collection) {
    super(context);

    this.collection = initial;
  }

  protected async render() {
    const current = await this.items.get(this.position);

    if (current) {
      this.collection = current;
    } else {
      this.position = Math.max(0, Math.min(this.items.cachedLength - 1, this.position));
    }

    return CollectionListMessage.renderStatic(this.context.locale, this.collection);
  }

  static renderStatic(locale: Locale, collection: Collection): RenderResult {
    return {
      embed: {
        title: collection.name || locale.get("collection.unnamed"),
        description: collection.description || locale.get("collection.noDesc"),
        url: getWebLink(`collections/${collection.id}`),
        color: "AQUA",
        footer: {
          text: `${collection.id} (${collection.type}, ${locale.get("collection.list.itemCount", {
            count: collection.items.length,
          })})`,
        },
      },
    };
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new ListTrigger(this, "left"),
      new ListTrigger(this, "right"),
      new DestroyTrigger(this),
    ];
  }
}
