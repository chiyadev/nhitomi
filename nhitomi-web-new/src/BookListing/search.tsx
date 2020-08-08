import { SortDirection, BookSort, BookQuery, QueryMatchMode } from 'nhitomi-api'
import { tokenize } from './SearchInput'

export type SearchQuery = {
  query?: string
  sort?: BookSort
  order?: SortDirection
}

export function convertQuery({ query, order, sort }: SearchQuery): BookQuery {
  const result: BookQuery = {
    limit: 50,
    mode: QueryMatchMode.All,
    tags: {},
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
          (result.tags![token.tag] || (result.tags![token.tag] = { values: [], mode: QueryMatchMode.All })).values.push(`"${value}"`) // tags are wrapped in quotes for phrase match

        break
      }
    }
  }

  return result
}
