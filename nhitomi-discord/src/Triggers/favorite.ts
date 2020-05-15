import { ReactionTrigger } from '../interactive'
import { ObjectType, SpecialCollection, CollectionInsertPosition } from 'nhitomi-api'
import { MessageContext } from '../context'

export type FavoriteTriggerTarget = {
  favoriteObject?: {
    id: string
    name: string
  }
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
    if (!this.target.favoriteObject)
      return false

    const itemId = this.target.favoriteObject.id
    const itemName = this.target.favoriteObject.name

    const l = context.locale.section('reaction.favorite')
    const { body: { id: collectionId, items } } = await context.api.user.getUserSpecialCollection(context.user.id, this.type, this.collection)

    if (items.includes(itemId)) {
      await context.api.collection.removeCollectionItems(collectionId, false, {
        items: [itemId]
      })

      context.scheduleDelete(await context.reply(l.get('remove', { name: itemName })))
    }
    else {
      await context.api.collection.addCollectionItems(collectionId, false, {
        items: [itemId],
        position: CollectionInsertPosition.Start
      })

      context.scheduleDelete(await context.reply(l.get('add', { name: itemName })))
    }

    return true
  }
}
