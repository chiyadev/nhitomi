import { BookOutlined } from '@ant-design/icons'
import { Empty, PageHeader } from 'antd'
import React, { Dispatch, useCallback, useContext, useRef } from 'react'
import { useAsync } from 'react-use'
import { Book, BookQuery, BookSort, SortDirection } from '../Client'
import { useTabTitle } from '../hooks'
import { Prefetch, PrefetchLink, PrefetchLinkProps, usePrefetch } from '../Prefetch'
import { ProgressContext } from '../Progress'
import { useScrollShortcut } from '../shortcuts'
import { GridListing } from './Grid'
import { Search } from './Search'
import { ClientContext } from '../ClientContext'
import { LayoutContent } from '../Layout'

type Fetched = {
  query: BookQuery
  pending: boolean

  /** this contains all items including ones loaded by infinite-scroll */
  items: Book[]

  total: number
  selected?: string
}

export function getBookListingPrefetch(): Prefetch<Fetched> {
  return {
    path: '/books',

    func: async client => {
      const query: BookQuery = {
        offset: 0,
        limit: 50,
        sorting: [{
          value: BookSort.CreatedTime,
          direction: SortDirection.Descending
        }]
      }

      const result = await client.book.searchBooks({ bookQuery: query })

      return {
        query,
        pending: false,
        items: result.items,
        total: result.total
      }
    }
  }
}

export const BookListing = () => {
  const { result, dispatch } = usePrefetch(getBookListingPrefetch())

  if (result)
    return <Loaded {...result} dispatch={dispatch} />

  return null
}

export const BookListingLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getBookListingPrefetch()} {...props} />

const Loaded = ({ dispatch, ...fetched }: Fetched & { dispatch: Dispatch<Fetched> }) => {
  const { query, pending, items, selected, total } = fetched

  useTabTitle('Books')
  useScrollShortcut()

  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)

  const queryId = useRef(0)

  // search handler
  useAsync(async () => {
    if (!pending)
      return

    start()

    try {
      const id = ++queryId.current

      const result = await client.book.searchBooks({
        bookQuery: {
          ...query,
          offset: 0,
          limit: 50
        }
      })

      // query may have changed while we were pending response
      if (id !== queryId.current)
        return

      dispatch({
        query,
        pending: false,
        items: result.items,
        total: result.total
      })
    }
    finally {
      stop()
    }
  }, [query, pending])

  // 'fetched' isn't a dep because dispatch func gets changed with it
  const setItems = useCallback((v: Book[]) => dispatch({ ...fetched, items: v }), [dispatch]) // eslint-disable-line
  const setSelected = useCallback((v?: string) => dispatch({ ...fetched, selected: v }), [dispatch]) // eslint-disable-line

  return <>
    <PageHeader
      avatar={{ icon: <BookOutlined />, shape: 'square' }}
      title='Books'
      subTitle='List of all books'
      extra={
        <Search
          query={query}
          setQuery={v => dispatch({ ...fetched, query: v, pending: true })}
          total={total} />} />

    <LayoutContent>
      {items.length
        ? <GridListing
          items={items}
          setItems={setItems}
          selected={selected}
          setSelected={setSelected} />

        : <Empty description='No results' />}
    </LayoutContent>
  </>
}
