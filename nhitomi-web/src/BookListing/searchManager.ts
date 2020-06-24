import { BookTag, Book, LanguageType, Client, BookQuery, QueryMatchMode, BookQueryTags, BookSort, SortDirection, BookContent, BookSearchResult, ScraperType } from '../Client'
import qs from 'qs'
import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'

export type SearchQuery = {
  language: LanguageType
  sources: ScraperType[]
} & ({
  type: 'simple'
  value: string
} | {
  type: 'tag'
  items: TagQueryItem[]
})

export type TagQueryItem = {
  type: BookTag
  value: string
}

export type SearchResult = {
  query: SearchQuery
  items: Book[]
  total: number
  end: boolean
  selected?: {
    book: Book
    content: BookContent
  }
}

export function serializeQuery(query: SearchQuery): string {
  const language = query.language.split('-')[0] || 'en'
  const sources = query.sources?.length ? query.sources.join(',') : undefined

  switch (query.type) {
    case 'tag':
      return qs.stringify({
        t: 't',
        l: language,
        s: sources,
        q: query.items.map(v => `${v.type}:${v.value}`).join(',') || undefined
      })

    case 'simple':
      return qs.stringify({
        t: 's',
        l: language,
        s: sources,
        q: query.value || undefined
      })
  }
}

export function deserializeQuery(query: string): SearchQuery {
  const { t, l, s, q } = qs.parse(query, { ignoreQueryPrefix: true }) as {
    t?: string
    l?: string
    s?: string
    q?: string
  }

  const language = Object.values(LanguageType).find(v => v.split('-')[0] === l) || LanguageType.EnUS
  const sources = Object.values(ScraperType).filter(t => (s || '').split(',').indexOf(t) !== -1)

  switch (t) {
    case 't':
      return {
        type: 'tag',
        language,
        sources,
        items: q?.split(',').map(v => {
          const delimiter = v.indexOf(':')

          if (delimiter === -1)
            return { type: BookTag.Tag, value: '' }

          return {
            type: v.substring(0, delimiter) as BookTag,
            value: v.substring(delimiter + 1)
          }
        }).filter(v => v.value) || []
      }

    case 's':
      return {
        type: 'simple',
        language,
        sources,
        value: q || ''
      }
  }

  return {
    type: 'tag',
    language: LanguageType.EnUS,
    sources,
    items: []
  }
}

export function convertQuery(query: SearchQuery, offset?: number): BookQuery {
  return {
    all: query.type !== 'simple' || !query.value ? undefined : {
      values: [query.value],
      mode: QueryMatchMode.All
    },
    language: {
      values: [query.language]
    },
    source: !query.sources?.length ? undefined : {
      values: query.sources,
      mode: QueryMatchMode.Any
    },
    tags: query.type !== 'tag' ? undefined : query.items.reduce((tags, { type, value }) => {
      const tag = tags[type] || (tags[type] = { values: [], mode: QueryMatchMode.All })

      tag.values.push(value)

      return tags
    }, {} as BookQueryTags),
    offset,
    limit: 50,
    sorting: [{
      value: BookSort.UpdatedTime,
      direction: SortDirection.Descending
    }]
  }
}

export function queriesEqual(a: SearchQuery, b: SearchQuery) {
  return a === b || serializeQuery(a) === serializeQuery(b)
}

// selects a book with the same id from the given list of books
function selectBook(books: Book[], selected: SearchResult['selected']): SearchResult['selected'] {
  for (const book of books) {
    if (book.id === selected?.book.id) {
      for (const content of book.contents) {
        if (content.id === selected.content.id)
          return { book, content }
      }
    }
  }
}

export class SearchManager extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, {
  loading: (loading: boolean) => void
  query: (query: SearchQuery, push: boolean) => void // push means query was performed by the user and new history entry will be created
  result: (result: SearchResult) => void
  failed: (error: Error) => void
}>) {
  private _query: SearchQuery
  private _result: SearchResult

  get query(): SearchQuery { return this._query }
  set query(v: SearchQuery) { this.emit('query', this._query = v, true) }

  get result(): SearchResult { return this._result }
  set result(v: SearchResult) { this.emit('result', this._result = v) }

  constructor(readonly client: Client) {
    super()

    this._query = {
      language: LanguageType.EnUS,
      sources: [],
      type: 'tag',
      items: []
    }

    this._result = {
      query: this.query,
      items: [],
      total: 0,
      end: true
    }
  }

  replace(query: SearchQuery, result: SearchResult) {
    if (!queriesEqual(this.query, query))
      this.emit('query', this._query = query, false)

    if (this.result !== result)
      this.emit('result', this._result = result)

    if (!queriesEqual(result.query, query))
      this.refresh()
  }

  toggleTag({ type, value }: TagQueryItem) {
    switch (this.query.type) {
      case 'simple': {
        // wrap around quotes for phrase match
        if (value.indexOf(' ') !== -1)
          value = `"${value}"`

        const exists = this.query.value.indexOf(value)

        this.query = {
          ...this.query,
          value: exists === -1
            ? `${this.query.value} ${value}`
            : this.query.value.substring(0, exists) + this.query.value.substring(exists + value.length)
        }

        break
      }

      case 'tag': {
        for (const item of this.query.items) {
          if (item.type === type && item.value === value) {
            this.query = {
              ...this.query,
              items: this.query.items.filter(v => v !== item)
            }
            return
          }
        }

        this.query = {
          ...this.query,
          items: [...this.query.items, { type, value }]
        }

        break
      }
    }
  }

  async search(query: SearchQuery, offset?: number): Promise<BookSearchResult> {
    // if simple query, try finding links first
    if (query.type === 'simple' && query.value) {
      const { matches } = await this.client.book.getBooksByLink({ strict: false, getBookByLinkRequest: { link: query.value } })

      if (matches.length) {
        return {
          took: '', // temp
          total: matches.length,
          items: matches.map(m => m.book)
        }
      }
    }

    return await this.client.book.searchBooks({ bookQuery: convertQuery(query, offset) })
  }

  canRefresh = true

  /** Searches from the start. */
  async refresh() {
    if (!this.canRefresh)
      return this.result

    this.emit('loading', true)

    const query = this.query

    try {
      const { items, total } = await this.search(query)

      if (queriesEqual(this.query, query)) {
        this.result = {
          ...this.result,
          query,
          items,
          total,
          end: items.length >= total,
          selected: selectBook(items, this.result.selected)
        }
      }
    }
    catch (e) {
      if (queriesEqual(this.query, query)) {
        this.result = { ...this.result, end: true }
        this.emit('failed', e)
      }
    }
    finally {
      this.emit('loading', false)
    }

    return this.result
  }

  /** Loads more results after the current page. */
  async further() {
    if (!this.canRefresh)
      return this.result

    this.emit('loading', true)

    const query = this.result.query

    try {
      const { items, total } = await this.search(this.query, this.result.items.length)

      if (queriesEqual(this.query, query)) {
        const totalItems = [
          ...this.result.items,
          ...items
        ]

        this.result = {
          ...this.result,
          query,
          items: totalItems,
          total,
          end: totalItems.length >= total,
          selected: selectBook(totalItems, this.result.selected)
        }
      }
    }
    catch (e) {
      if (queriesEqual(this.query, query)) {
        this.result = { ...this.result, end: true }
        this.emit('failed', e)
      }
    }
    finally {
      this.emit('loading', false)
    }

    return this.result
  }
}
