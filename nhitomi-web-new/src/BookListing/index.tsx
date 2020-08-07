import React, { useState, Dispatch } from 'react'
import { SearchQuery, convertQuery } from './search'
import { useUrlState } from '../url'
import { Prefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { Book } from 'nhitomi-api'

export function getBookListingPrefetch(query?: SearchQuery): Prefetch<Book[], { query: SearchQuery, setQuery: Dispatch<SearchQuery> }> {
  return {
    path: '/books',

    useData: mode => {
      const [currentQuery, setQuery] = useUrlState<SearchQuery>()

      return { query: query || (mode === 'postfetch' && currentQuery) || {}, setQuery }
    },

    fetch: async (client, _, { query }) => {
      return (await client.book.searchBooks({ bookQuery: convertQuery(query) })).items
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
  return (
    <BookListing />
  )
}

export const BookListing = () => {
  const [query, setQuery] = useUrlState<SearchQuery>('push')

  return <>
  </>
}
