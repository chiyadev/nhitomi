import { ReactionTrigger } from '../interactive'
import { ObjectType, SpecialCollection, CollectionInsertPosition } from 'nhitomi-api'
import { MessageContext } from '../context'

export type FavoriteTriggerTarget = {
  favoriteObject?: { id: string }
}

export class FavoriteTrigger extends ReactionTrigger {
  readonly emoji = '\u2764'

  constructor(
    readonly target: FavoriteTriggerTarget,
    readonly type: ObjectType,
    readonly collection: SpecialCollection
  ) {
    super()
  }

  protected async run(context: MessageContext): Promise<boolean> {
    const itemId = this.target.favoriteObject?.id

    if (!itemId)
      return false

    const { body: { id: collectionId, items } } = await context.api.user.getUserSpecialCollection(context.user.id, this.type, this.collection)

    if (items.includes(itemId)) {
      await context.api.collection.removeCollectionItems(collectionId, false, {
        items: [itemId]
      })
    }
    else {
      await context.api.collection.addCollectionItems(collectionId, false, {
        items: [itemId],
        position: CollectionInsertPosition.Start
      })
    }

    return true
  }
}
