import React, { Dispatch, useMemo } from 'react'
import { SearchQuery, convertQuery } from './search'
import { useUrlState } from '../url'
import { Prefetch, PrefetchLinkProps, PrefetchLink, usePostfetch } from '../Prefetch'
import { Book, BookSearchResult } from 'nhitomi-api'
import { SearchInput } from './SearchInput'

export type PrefetchResult = BookSearchResult

export function getBookListingPrefetch(query?: SearchQuery): Prefetch<PrefetchResult, { query: SearchQuery, setQuery: Dispatch<SearchQuery> }> {
  return {
    path: '/books',

    useData: mode => {
      const [currentQuery, setQuery] = useUrlState<SearchQuery>()

      return { query: query || (mode === 'postfetch' && currentQuery) || {}, setQuery }
    },

    fetch: async (client, _, { query }) => {
      return await client.book.searchBooks({ bookQuery: convertQuery(query) })
    },

    done: (_, __, ___, { query, setQuery }) => {
      setQuery(query)
    }
  }
}

export const BookListingLink = ({ query, ...props }: Omit<PrefetchLinkProps, 'fetch'> & { query?: SearchQuery }) => (
  <PrefetchLink fetch={getBookListingPrefetch(query)} {...props} />
)

export const BookListingPage = () => {
  const { result } = usePostfetch(useMemo(() => getBookListingPrefetch(), []))

  if (!result)
    return null

  return (
    <BookListing items={result.items} />
  )
}

export const BookListing = ({ items }: { items: Book[] }) => {
  return <>
    <SearchInput />

    {items.map(x => <>
      <p>{x.englishName}</p>
      <p>{x.primaryName}</p>
    </>)}
  </>
}
