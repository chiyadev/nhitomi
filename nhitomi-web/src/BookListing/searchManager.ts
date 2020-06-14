import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types/types/src'
import { BookTag, Book, BookQuery, BookSort, SortDirection, BookQueryTags, QueryMatchMode, Client, LanguageType } from '../Client'

export type TagQueryItem = {
  type: BookTag
  value: string
}

export type SearchState = {
  id: number
  items: Book[]
  total: number
  simpleQuery?: string
  tagQuery: TagQueryItem[]
  language: LanguageType
}

export class SearchManager extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, {
  items: (items: Book[]) => void
  total: (total: number) => void

  simpleQuery: (query?: string) => void
  tagQuery: (items: TagQueryItem[]) => void
  language: (language: LanguageType) => void

  loading: (loading: boolean) => void
}>) {
  state: SearchState = {
    id: 1,
    items: [],
    total: 0,
    tagQuery: [],
    language: LanguageType.EnUS
  }

  get items(): Book[] { return this.state.items }
  set items(v: Book[]) { this.emit('items', this.state.items = v) }

  get total(): number { return this.state.total }
  set total(v: number) { this.emit('total', this.state.total = v) }

  get simpleQuery(): string | undefined { return this.state.simpleQuery }
  set simpleQuery(v: string | undefined) { this.emit('simpleQuery', this.state.simpleQuery = v) }

  get tagQuery(): TagQueryItem[] { return this.state.tagQuery }
  set tagQuery(v: TagQueryItem[]) { this.emit('tagQuery', this.state.tagQuery = v) }

  get language(): LanguageType { return this.state.language }
  set language(v: LanguageType) { this.emit('language', this.state.language = v) }

  constructor(readonly client: Client) {
    super()

    this.on('simpleQuery', () => this.refresh())
    this.on('tagQuery', () => this.refresh())
    this.on('language', () => this.refresh())
  }

  setState(state: SearchState) {
    if (this.state.id === state.id)
      return

    for (const key in state)
      (this as any)[key] = (state as any)[key]
  }

  toggleTag({ type, value }: TagQueryItem) {
    for (const item of this.tagQuery) {
      if (item.type === type && item.value === value) {
        this.tagQuery = this.tagQuery.filter(v => v !== item)
        return
      }
    }

    this.tagQuery = [...this.tagQuery, { type, value }]
  }

  createQuery(): BookQuery {
    return {
      all: !this.simpleQuery ? undefined : {
        values: [this.simpleQuery]
      },
      tags: this.tagQuery.reduce((tags, { type, value }) => {
        const tag = tags[type] || (tags[type] = { values: [], mode: QueryMatchMode.All })

        tag.values.push(value)

        return tags
      }, {} as BookQueryTags),
      limit: 50,
      sorting: [{
        value: BookSort.CreatedTime,
        direction: SortDirection.Descending
      }]
    }
  }

  /** Searches from the start. */
  async refresh() {
    this.emit('loading', true)

    try {
      const id = ++this.state.id

      const { items, total } = await this.client.book.searchBooks({ bookQuery: this.createQuery() })

      if (this.state.id === id) {
        this.items = items
        this.total = total
      }

      return this.items
    }
    finally {
      this.emit('loading', false)
    }
  }
}
