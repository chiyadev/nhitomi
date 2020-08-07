import { SortDirection, BookSort, BookQuery } from 'nhitomi-api'

export type SearchQuery = {
  query?: string
  sort?: BookSort
  order?: SortDirection
}

export function convertQuery(query: SearchQuery): BookQuery {
  return {
    limit: 50,
    sorting: [{
      value: query.sort || BookSort.UpdatedTime,
      direction: query.order || SortDirection.Descending
    }]
  }
}
