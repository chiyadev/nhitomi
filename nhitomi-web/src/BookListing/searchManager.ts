import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'
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
  state: (state: SearchState) => void
  refresh: () => void
}>) {
  state: SearchState = {
    id: Math.random(),
    items: [],
    end: true,
    total: 0,
    tagQuery: [],
    language: LanguageType.EnUS
  }

  get id(): number { return this.state.id }
  set id(v: number) { this.emit('state', { ...this.state, id: v }) }

  get items(): Book[] { return this.state.items }
  set items(v: Book[]) { this.emit('state', { ...this.state, items: v }) }

  get total(): number { return this.state.total }
  set total(v: number) { this.emit('state', { ...this.state, total: v }) }

  get end(): boolean { return this.state.end }
  set end(v: boolean) { this.emit('state', { ...this.state, end: v }) }

  get simpleQuery(): string | undefined { return this.state.simpleQuery }
  set simpleQuery(v: string | undefined) { this.emit('state', { ...this.state, simpleQuery: v }); this.emit('refresh') }

  get tagQuery(): TagQueryItem[] { return this.state.tagQuery }
  set tagQuery(v: TagQueryItem[]) { this.emit('state', { ...this.state, tagQuery: v }); this.emit('refresh') }

  get language(): LanguageType { return this.state.language }
  set language(v: LanguageType) { this.emit('state', { ...this.state, language: v }); this.emit('refresh') }

  constructor(readonly client: Client) {
    super()

    this.on('state', s => this.state = s)
    this.on('refresh', () => this.refreshOnce())
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

  canRefresh = false

  /** Searches from the start. */
  async refresh() {
    if (!this.canRefresh)
      return this.items

    this.emit('loading', true)

    try {
      const id = ++this.id

      const { items, total } = await this.client.book.searchBooks({ bookQuery: this.createQuery() })

      if (this.id === id) {
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

  private refreshTask?: number

  refreshOnce() {
    if (this.refreshTask || !this.canRefresh)
      return

    this.refreshTask = setTimeout(() => {
      this.refreshTask = undefined
      this.refresh()
    })
  }

  /** Loads more results after the current page. */
  async further() {
    this.emit('loading', true)

    try {
      const id = ++this.id

      const { items, total } = await this.client.book.searchBooks({ bookQuery: this.createQuery(this.state.items.length) })

      if (this.id === id) {
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
