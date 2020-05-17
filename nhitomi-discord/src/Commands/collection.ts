import { CommandFunc } from '.'
import { handleGetLink, replyNotFound } from './get'
import { ObjectType, CollectionInsertPosition } from 'nhitomi-api'

const regex = /(?<collection>.+)\s+(?<command>a(dd)?|r(emove)?|delete)(\s+(?<link>.+))?/ms

export const run: CommandFunc = async (context, arg) => {
  const match = regex.exec(arg || '')
  const command = match?.groups?.command?.trim().toLowerCase() || ''
  const collectionName = match?.groups?.collection?.trim() || ''
  const link = match?.groups?.link?.trim() || ''

  let { items: collections } = await context.api.user.getUserCollections({ id: context.user.id })
  collections = collections.filter(c => c.name?.toLowerCase() === collectionName.toLowerCase())

  let collection = collections[0]

  switch (command) {
    case 'a':
    case 'r':
    case 'add':
    case 'remove': {
      const linkResult = await handleGetLink(context, link)

      if (linkResult.type === 'notFound') {
        await replyNotFound(context, link)
        return true
      }

      if (!collection)
        collection = await context.api.collection.createCollection({
          createCollectionRequest: {
            type: linkResult.type,
            collection: {
              name: collectionName
            }
          }
        })

      let itemId: string
      let itemName: string

      switch (linkResult.type) {
        case ObjectType.Book:
          itemId = linkResult.book.id
          itemName = linkResult.book.primaryName
          break
      }

      switch (command) {
        case 'a':
        case 'add': {
          const l = context.locale.section('collection.add')

          if (collection.items.includes(itemId))
            await context.reply(l.get('exists', { item: itemName, collection: collection.name }))

          else {
            await context.api.collection.addCollectionItems({
              id: collection.id,
              addCollectionItemsRequest: {
                items: [itemId],
                position: CollectionInsertPosition.Start
              }
            })

            await context.reply(l.get('success', { item: itemName, collection: collection.name }))
          }

          return true
        }

        case 'r':
        case 'remove': {
          const l = context.locale.section('collection.remove')

          if (!collection.items.includes(itemId))
            await context.reply(l.get('notExists', { item: itemName, collection: collection.name }))

          else {
            await context.api.collection.removeCollectionItems({
              id: collection.id,
              collectionItemsRequest: {
                items: [itemId]
              }
            })

            await context.reply(l.get('success', { item: itemName, collection: collection.name }))
          }

          return true
        }
      }

      break
    }

    case 'delete':
      if (collection) {
        const l = context.locale.section('collection.delete')

        const confirm = await context.waitInput(l.get('confirm', { collection: collection.name }))

        if (!'yes'.startsWith(confirm.trim().toLowerCase() || 'no'))
          return true

        await context.api.collection.deleteCollection({ id: collection.id })

        await context.reply(l.get('success', { collection: collection.name }))
      }

      break
  }

  return true
}
