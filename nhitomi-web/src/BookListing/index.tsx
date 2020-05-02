import { BookOutlined } from '@ant-design/icons'
import { Empty, PageHeader } from 'antd'
import React, { Dispatch, useCallback, useContext, useRef, useState, createContext, useMemo } from 'react'
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
    return <Loaded fetched={result} dispatch={dispatch} />

  return null
}

export const BookListingLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getBookListingPrefetch()} {...props} />

export const BookQueryContext = createContext<{
  query: BookQuery
  setQuery: (q: BookQuery) => void
}>(undefined as any)

const Loaded = ({ fetched, dispatch }: { fetched: Fetched, dispatch: Dispatch<Fetched> }) => {
  const { query, pending, items, total } = fetched

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

  const [selected, setSelected] = useState<string>()
  const setQuery = useCallback((v: BookQuery) => dispatch({ ...fetched, query: v, pending: true }), [dispatch, fetched])
  const setItems = useCallback((v: Book[]) => dispatch({ ...fetched, items: v }), [dispatch, fetched])

  return <BookQueryContext.Provider value={useMemo(() => ({
    query,
    setQuery
  }), [
    query,
    setQuery
  ])}>

    <PageHeader
      avatar={{ icon: <BookOutlined />, shape: 'square' }}
      title='Books'
      subTitle='List of all books'
      extra={<Search query={query} setQuery={setQuery} total={total} />} />

    <LayoutContent>
      {items.length
        ? <GridListing
          items={items}
          setItems={setItems}
          selected={selected}
          setSelected={setSelected} />

        : <Empty description='No results' />}
    </LayoutContent>
  </BookQueryContext.Provider>
}
