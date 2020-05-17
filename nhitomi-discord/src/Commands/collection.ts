import { CommandFunc } from '.'
import { handleGetLink, replyNotFound } from './get'
import { ObjectType, CollectionInsertPosition, Collection } from 'nhitomi-api'

const regex = /(?<collection>.+)\s+(?<command>a(dd)?|r(emove)?|delete)(\s+(?<link>.+))?/ms

export const run: CommandFunc = async (context, arg) => {
  const match = regex.exec(arg || '')
  const command = match?.groups?.command?.trim().toLowerCase() || ''
  const collectionName = match?.groups?.collection?.trim() || ''
  const link = match?.groups?.link?.trim() || ''

  let { items: collections } = await context.api.user.getUserCollections({ id: context.user.id })
  collections = collections.filter(c => c.name?.toLowerCase().startsWith(collectionName.toLowerCase()))

  let collection: Collection | undefined = collections[0]

  // ambiguous collection match
  if (collectionName && collections.length > 1) {
    const l = context.locale.section('collection.select')

    const selected = await context.waitInput(`
${l.get('message')}

${collections.map((c, i) => {
      let name = `${i + 1}. \`${c.name}\``

      if (collections.some(c2 => c !== c2 && c.name === c2.name))
        name = `${name} (${c.id} ${c.type})`

      return name
    }).join('\n')}
`.trim())

    const index = parseInt(selected) - 1

    if (isNaN(index))
      collection = collections.find(c => c.name?.toLowerCase().startsWith(selected.toLowerCase()))
    else
      collection = collections[Math.max(0, Math.min(collections.length - 1, index))]

    if (!collection)
      return true
  }

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
