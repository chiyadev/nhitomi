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
  end: boolean
  simpleQuery?: string
  tagQuery: TagQueryItem[]
  language: LanguageType
}

export class SearchManager extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, {
  loading: (loading: boolean) => void

  items: (items: Book[]) => void
  total: (total: number) => void
  end: (end: boolean) => void

  simpleQuery: (query?: string) => void
  tagQuery: (items: TagQueryItem[]) => void
  language: (language: LanguageType) => void

  state: (state: SearchState) => void
}>) {
  state: SearchState = {
    id: Math.random(),
    items: [],
    end: false,
    total: 0,
    tagQuery: [],
    language: LanguageType.EnUS
  }

  get items(): Book[] { return this.state.items }
  set items(v: Book[]) { this.emit('items', this.state.items = v); this.emit('state', this.state) }

  get total(): number { return this.state.total }
  set total(v: number) { this.emit('total', this.state.total = v); this.emit('state', this.state) }

  get end(): boolean { return this.state.end }
  set end(v: boolean) { this.emit('end', this.state.end = v); this.emit('state', this.state) }

  get simpleQuery(): string | undefined { return this.state.simpleQuery }
  set simpleQuery(v: string | undefined) { if (v === this.simpleQuery) return; this.emit('simpleQuery', this.state.simpleQuery = v); this.emit('state', this.state); this.refresh() }

  get tagQuery(): TagQueryItem[] { return this.state.tagQuery }
  set tagQuery(v: TagQueryItem[]) { if (v === this.tagQuery) return; this.emit('tagQuery', this.state.tagQuery = v); this.emit('state', this.state); this.refresh() }

  get language(): LanguageType { return this.state.language }
  set language(v: LanguageType) { if (v === this.language) return; this.emit('language', this.state.language = v); this.emit('state', this.state); this.refresh() }

  constructor(readonly client: Client) { super() }

  setState(state: SearchState) {
    if (this.state.id === state.id)
      return

    this.emit('state', this.state = state)

    for (const key in state)
      this.emit(key as any, (state as any)[key])
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

  createQuery(offset?: number): BookQuery {
    return {
      all: !this.simpleQuery ? undefined : {
        values: [this.simpleQuery]
      },
      language: {
        values: [this.language]
      },
      tags: this.tagQuery.reduce((tags, { type, value }) => {
        const tag = tags[type] || (tags[type] = { values: [], mode: QueryMatchMode.All })

        tag.values.push(value)

        return tags
      }, {} as BookQueryTags),
      offset,
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
        this.end = items.length >= total
      }

      return this.items
    }
    finally {
      this.emit('loading', false)
    }
  }

  /** Loads more results after the current page. */
  async further() {
    this.emit('loading', true)

    try {
      const id = ++this.state.id

      const { items, total } = await this.client.book.searchBooks({ bookQuery: this.createQuery(this.state.items.length) })

      if (this.state.id === id) {
        this.items = [...this.items, ...items]
        this.total = total
        this.end = this.items.length >= total
      }

      return this.items
    }
    finally {
      this.emit('loading', false)
    }
  }
}
