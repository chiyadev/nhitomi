import React, { Dispatch, useMemo, useRef, useLayoutEffect } from 'react'
import { SearchQuery, convertQuery } from './search'
import { useUrlState } from '../url'
import { Prefetch, PrefetchLinkProps, PrefetchLink, usePostfetch, usePageState } from '../Prefetch'
import { BookSearchResult } from 'nhitomi-api'
import { SearchInput } from './SearchInput'
import { BookList } from '../Components/BookList'
import { useAsync } from 'react-use'
import { useNotify } from '../NotificationManager'
import { useClient } from '../ClientManager'
import { LoadContainer } from '../Components/LoadContainer'
import { useProgress } from '../ProgressManager'

export type PrefetchResult = BookSearchResult

export function getBookListingPrefetch(query?: SearchQuery): Prefetch<PrefetchResult, { query: SearchQuery, setQuery: Dispatch<SearchQuery> }> {
  return {
    path: '/books',

    useData: mode => {
      const [currentQuery, setQuery] = useUrlState<SearchQuery>()
      const [, setEffectiveQuery] = usePageState<SearchQuery>('query')

      return {
        // use query specified by caller, or the current query in url if refreshing, or an empty query if coming from another page
        query: query || (mode === 'postfetch' && currentQuery) || {},

        setQuery: q => {
          // set query in url because prefetch will navigate to /books and clear the query part
          setQuery(q)

          // synchronize effective query since we are immediately displaying the results after load
          setEffectiveQuery(q)
        }
      }
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

export const BookListing = () => {
  const { result, dispatch } = usePostfetch(useMemo(() => getBookListingPrefetch(), []))

  if (!result)
    return null

  return (
    <Loaded result={result} setResult={dispatch} />
  )
}

const Loaded = ({ result, setResult }: { result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end } = useProgress()

  const [query] = useUrlState<SearchQuery>()
  const queryId = useRef(0)

  const [effectiveQuery, setEffectiveQuery] = usePageState<SearchQuery>('query') // displayed results may not represent the current query if we navigated before storing the results

  // serialized query string is used for comparison
  const queryCmp = useMemo(() => JSON.stringify(query), [query])
  const effectiveQueryCmp = useMemo(() => JSON.stringify(effectiveQuery || {}), [effectiveQuery])

  // perform search when query changes
  useAsync(async () => {
    if (queryCmp === effectiveQueryCmp)
      return

    begin()

    const id = ++queryId.current

    try {
      const result = await client.book.searchBooks({ bookQuery: convertQuery(query) })

      if (queryId.current === id) {
        setResult(result)
        setEffectiveQuery(query)
      }
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      end()
    }
  }, [queryCmp, effectiveQueryCmp])

  return <>
    <div className='mx-auto p-4 w-full max-w-xl sticky top-0 z-20'>
      <SearchInput className='shadow-lg w-full' />
    </div>

    <BookList items={result.items} />
    <Loader key={queryCmp} query={query} result={result} setResult={setResult} />
  </>
}

const Loader = ({ query, result, setResult }: { query: SearchQuery, result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end: endProgress } = useProgress()

  const [end, setEnd] = usePageState<boolean>('end')
  const count = useRef(0)

  // unmount means query changed, so prevent setting irrelevant results
  useLayoutEffect(() => () => { count.current = -1 }, [])

  if (end)
    return null

  return (
    <LoadContainer
      key={count.current} // recreate load container for each load
      className='w-full h-20'
      onLoad={async () => {
        begin()

        try {
          const moreResult = await client.book.searchBooks({ bookQuery: { ...convertQuery(query), offset: result.items.length } })

          if (count.current >= 0) {
            if (!moreResult.items.length) {
              setEnd(true)
              return
            }

            setResult({
              ...moreResult,
              items: [
                ...result.items,
                ...moreResult.items
              ]
            })

            ++count.current
          }
        }
        catch (e) {
          notifyError(e)
        }
        finally {
          endProgress()
        }
      }} />
  )
}
