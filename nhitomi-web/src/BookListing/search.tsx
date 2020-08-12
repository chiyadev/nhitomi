import { SortDirection, BookSort, BookQuery, QueryMatchMode, LanguageType } from 'nhitomi-api'
import { tokenize } from './SearchInput'

export type SearchQuery = {
  query?: string
  sort?: BookSort
  order?: SortDirection
  langs?: LanguageType[]
}

export function convertQuery({ query, order, sort, langs }: SearchQuery): BookQuery {
  const result: BookQuery = {
    limit: 50,
    mode: QueryMatchMode.All,
    tags: {},
    language: !langs?.length ? undefined : {
      values: langs,
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
          (result.tags![token.tag] || (result.tags![token.tag] = { values: [], mode: QueryMatchMode.All })).values.push(`"${value}"`) // tags are wrapped in quotes for phrase match

        break
      }
    }
  }

  return result
}
