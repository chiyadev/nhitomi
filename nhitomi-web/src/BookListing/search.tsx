import { SortDirection, BookSort, BookQuery, QueryMatchMode, LanguageType, ScraperType } from 'nhitomi-api'
import { tokenize } from './SearchInput'
import { Client, ClientInfo } from '../ClientManager'

export const DefaultQueryLimit = 50

export type SearchQuery = {
  query?: string
  sort?: BookSort
  order?: SortDirection
  langs?: LanguageType[]
  sources?: ScraperType[]
}

export function convertQuery({ query, order, sort, langs, sources }: SearchQuery): BookQuery {
  const result: BookQuery = {
    limit: DefaultQueryLimit,
    mode: QueryMatchMode.All,
    tags: {},
    language: !langs?.length ? undefined : {
      values: langs,
      mode: QueryMatchMode.Any
    },
    source: !sources?.length ? undefined : {
      values: sources,
      mode: QueryMatchMode.Any
    },
    sorting: [{
      value: sort || BookSort.UpdatedTime,
      direction: order || SortDirection.Descending
    }]
  }

  for (const token of tokenize(query || '')) {
    switch (token.type) {
      case 'other': {
        if (token.display)
          (result.all || (result.all = { values: [], mode: QueryMatchMode.All })).values.push(token.display)

        break
      }

      case 'tag': {
        const value = token.display.substring(token.display.indexOf(':'))

        if (value)
          (result.tags![token.tag] || (result.tags![token.tag] = { values: [], mode: QueryMatchMode.All })).values.push(wrapTag(value))

        break
      }
    }
  }

  return result
}

function wrapTag(tag: string) {
  let negated = false

  if (tag.startsWith('-')) {
    negated = true
    tag = tag.substring(1)
  }

  // tags are wrapped in quotes for phrase match
  tag = `"${tag}"`

  if (negated)
    tag = '-' + tag

  return tag
}

export async function performQuery(client: Client, info: ClientInfo, query: SearchQuery) {
  // try scanning for links first
  if (query.query && info.scrapers.findIndex(s => !s.galleryRegexLax || query.query?.match(new RegExp(s.galleryRegexLax, 'gi'))?.length) !== -1) {
    const { matches } = await client.book.getBooksByLink({ getBookByLinkRequest: { link: query.query } })

    if (matches.length) {
      return {
        items: matches.map(match => match.book),
        took: '',
        total: matches.length
      }
    }
  }

  // if not, perform an actual search
  return await client.book.searchBooks({ bookQuery: convertQuery(query) })
}
