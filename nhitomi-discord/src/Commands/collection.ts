import { CommandFunc } from '.'
import { handleGetLink, replyNotFound } from './get'
import { ObjectType, CollectionInsertPosition, Collection, Book } from 'nhitomi-api'
import { BookListMessage } from './search'
import { AsyncArray } from '../asyncArray'
import config from 'config'
import { InteractiveMessage, RenderResult, ReactionTrigger } from '../interactive'
import { Locale } from '../locales'
import { Api } from '../api'
import { ListTrigger } from '../Triggers/list'
import { DestroyTrigger } from '../Triggers/destroy'

export class CollectionListMessage extends InteractiveMessage {
  constructor(readonly collections: Collection[]) { super() }

  position = 0
  collection?: Collection

  protected async render(l: Locale): Promise<RenderResult> {
    if (!(this.collection = this.collections[this.position = Math.max(0, Math.min(this.collections.length - 1, this.position))])) {
      l = l.section('list.empty')

      return {
        embed: {
          title: l.get('title'),
          description: l.get('message'),
          color: 'AQUA'
        }
      }
    }

    return CollectionListMessage.renderStatic(l, this.collection)
  }

  static renderStatic(l: Locale, collection: Collection): RenderResult {
    l = l.section('collection.list')

    return {
      embed: {
        title: collection.name || '<unnamed>',
        description: collection.description || '<no description>',
        url: Api.getWebLink(`collections/${collection.id}`),
        color: 'AQUA',
        footer: {
          text: `${collection.id} (${collection.type}, ${l.get('itemCount', { count: collection.items.length })})`
        }
      }
    }
  }

  protected createTriggers(): ReactionTrigger[] {
    if (!this.collection) return []

    return [
      ...super.createTriggers(),

      new ListTrigger(this, 'left'),
      new ListTrigger(this, 'right'),
      new DestroyTrigger()
    ]
  }
}

export class BookCollectionContentMessage extends BookListMessage {
  constructor(readonly collection: Collection) {
    super(new AsyncArray<Book>(config.get('search.chunkSize'), async (offset, limit) => {
      const ids = collection.items.slice(offset, offset + limit)

      if (!ids.length)
        return []

      const results = await this.context?.api.book.getBooks({ getBookManyRequest: { ids } })

      // collections can contain ids of deleted items
      return results?.filter(b => b) || []
    }))
  }

  protected processRenderResult(result: RenderResult): RenderResult {
    result = super.processRenderResult(result)

    result.message = `
> ${this.collection.name} — ${Api.getWebLink(`collections/${this.collection.id}`)}

${result.message}
`.trim()

    return result
  }
}

const commandRegex = /(?<collection>.+)\s+(?<command>add|remove|delete)(\s+(?<link>.+))?/ms

export const run: CommandFunc = async (context, arg) => {
  const match = commandRegex.exec(arg || '')
  const command = match?.groups?.command?.trim().toLowerCase() || ''
  const collectionName = match?.groups?.collection?.trim() || arg || ''
  const link = match?.groups?.link?.trim() || ''

  let { items: collections } = await context.api.user.getUserCollections({ id: context.user.id })
  collections = collections.filter(c => !collectionName || c.name.toLowerCase().startsWith(collectionName.toLowerCase()))

  let collection: Collection | undefined

  if (collectionName) {
    // ambiguous collection match
    if (collections.length > 1) {
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
        collection = collections.find(c => c.name.toLowerCase().startsWith(selected.toLowerCase()))
      else
        collection = collections[Math.max(0, Math.min(collections.length - 1, index))]

      if (!collection)
        return true
    }
    else {
      collection = collections[0]
    }
  }

  switch (command) {
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
        case 'add': {
          const l = context.locale.section('collection.add')

          if (collection.items.includes(itemId))
            await context.reply(l.get('exists', { item: itemName, collection: collection.name }))

          else {
            // ensure item type is the same as collection type
            if (collection.type !== linkResult.type) {
              const l = context.locale.section('collection.add.typeIncompatible')

              await context.reply(l.get('typeIncompatible', { item: itemName, itemType: linkResult.type, collection: collection.name, collectionType: collection.type }))
            }

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
          }

          break
        }

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

          break
        }
      }

      return true
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

      return true
  }

  switch (collection?.type) {
    case ObjectType.Book:
      return await new BookCollectionContentMessage(collection).initialize(context)
  }

  return await new CollectionListMessage(collections).initialize(context)
}
