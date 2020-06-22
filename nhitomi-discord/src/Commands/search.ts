import { CommandFunc } from '.'
import { InteractiveMessage, RenderResult, ReactionTrigger } from '../interactive'
import { Locale } from '../locales'
import { BookQuery, BookSort, SortDirection, Book, BookContent, ObjectType, SpecialCollection, QueryMatchMode } from 'nhitomi-api'
import { AsyncArray } from '../asyncArray'
import { BookMessage } from './get'
import { ReadTrigger } from '../Triggers/read'
import { DestroyTrigger } from '../Triggers/destroy'
import { ListTrigger } from '../Triggers/list'
import { FavoriteTrigger, FavoriteTriggerTarget } from '../Triggers/favorite'
import config from 'config'

export class BookListMessage extends InteractiveMessage {
  constructor(readonly items: AsyncArray<Book>) { super() }

  position = 0

  book?: Book
  content?: BookContent

  protected async render(l: Locale): Promise<RenderResult> {
    this.book = await this.items.get(this.position)

    if (!this.book && !(this.book = this.items.getCached(this.position = Math.max(0, Math.min(this.items.loadedLength - 1, this.position))))) {
      l = l.section('list.empty')

      return {
        embed: {
          title: l.get('title'),
          description: l.get('message'),
          color: 'AQUA'
        }
      }
    }

    this.content = this.book.contents.filter(c => c.language === this.context?.user.language)[0] || this.book.contents[0]

    return this.processRenderResult(BookMessage.renderStatic(l, this.book, this.content))
  }

  get favoriteObject(): FavoriteTriggerTarget['favoriteObject'] {
    return this.book ? {
      id: this.book.id,
      name: this.book.primaryName
    } : undefined
  }

  protected createTriggers(): ReactionTrigger[] {
    if (!this.book) return []

    return [
      ...super.createTriggers(),

      new FavoriteTrigger(this, ObjectType.Book, SpecialCollection.Favorites),
      new ReadTrigger(this),
      new ListTrigger(this, 'left'),
      new ListTrigger(this, 'right'),
      new DestroyTrigger()
    ]
  }

  protected processRenderResult(result: RenderResult): RenderResult { return result }
}

export class BookSearchMessage extends BookListMessage {
  constructor(query: BookQuery) {
    super(new AsyncArray<Book>(config.get('search.chunkSize'), async (offset, limit) => {
      const results = await this.context?.api.book.searchBooks({
        bookQuery: {
          ...query,
          offset,
          limit
        }
      })

      return results?.items || []
    }))
  }
}

export const run: CommandFunc = (context, query) => {
  const baseQuery: BookQuery = {
    all: !query ? undefined : {
      mode: QueryMatchMode.All,
      values: [query]
    },
    language: {
      values: [context.user.language]
    },
    limit: 0,
    sorting: [{
      value: BookSort.UpdatedTime,
      direction: SortDirection.Descending
    }]
  }

  return new BookSearchMessage(baseQuery).initialize(context)
}
