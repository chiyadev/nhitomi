import React, { Dispatch, useMemo, useRef, useLayoutEffect } from 'react'
import { SearchQuery, convertQuery } from './search'
import { useQueryState, usePageState } from '../state'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { BookSearchResult } from 'nhitomi-api'
import { SearchInput } from './SearchInput'
import { BookList } from '../Components/BookList'
import { useAsync } from 'react-use'
import { useNotify } from '../NotificationManager'
import { useClient } from '../ClientManager'
import { LoadContainer } from '../Components/LoadContainer'
import { useProgress } from '../ProgressManager'
import { Menu } from './Menu'
import { useScrollShortcut } from '../shortcut'
import { useConfig } from '../ConfigManager'
import { useSpring, animated } from 'react-spring'
import { PageContainer } from '../Components/PageContainer'

export type PrefetchResult = BookSearchResult
export type PrefetchOptions = { query?: SearchQuery }

export const useBookListingPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, query: targetQuery }) => {
  const client = useClient()
  const [languages] = useConfig('searchLanguages')
  const [currentQuery] = useQueryState<SearchQuery>()

  const query = targetQuery || (mode === 'postfetch' && currentQuery) || {}

  // use configured languages if unspecified
  query.langs = query.langs || languages

  return {
    destination: {
      path: '/books',
      query,
      state: s => ({
        ...s,
        query: { value: query, version: Math.random() } // synchronize effective query immediately
      })
    },

    fetch: async () => {
      return await client.book.searchBooks({ bookQuery: convertQuery(query) })
    }
  }
}

export const BookListingLink = ({ query, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useBookListingPrefetch} options={{ query }} {...props} />
)

export const BookListing = () => {
  const { result, setResult } = usePostfetch(useBookListingPrefetch, {})

  useScrollShortcut()

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded result={result} setResult={setResult} />
    </PageContainer>
  )
}

const Loaded = ({ result, setResult }: { result: BookSearchResult, setResult: Dispatch<BookSearchResult> }) => {
  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end } = useProgress()

  const [query] = useQueryState<SearchQuery>()
  const queryId = useRef(0)

  // displayed results may not represent the current query if we navigated before storing the results
  const [effectiveQuery, setEffectiveQuery] = usePageState<SearchQuery>('query')

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
    <Input result={result} />

    <BookList items={result.items}>
      <Menu />
    </BookList>

    <Loader key={queryCmp} query={query} result={result} setResult={setResult} />
  </>
}

const Input = ({ result }: { result: BookSearchResult }) => {
  const style = useSpring({
    from: { opacity: 0, marginTop: -5 },
    to: { opacity: 1, marginTop: 0 }
  })

  return (
    <div className='mx-auto p-4 w-full max-w-xl sticky top-0 z-20'>
      <animated.div style={style} className='w-full'>
        <SearchInput result={result} className='shadow-lg w-full' />
      </animated.div>
    </div>
  )
}

const Loader = ({ query, result, setResult }: { query: SearchQuery, result: BookSearchResult, setResult: Dispatch<BookSearchResult> }) => {
  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end: endProgress } = useProgress()

  const loadId = useRef(0)

  // unmount means query changed, so prevent setting irrelevant results
  useLayoutEffect(() => () => { loadId.current = -1 }, [])

  const style = useSpring({
    opacity: loadId.current < 0 ? 0 : 1
  })

  return (
    <animated.div style={style}>
      <LoadContainer
        key={loadId.current} // recreate load container for each load
        className='w-full h-20'
        onLoad={async () => {
          if (loadId.current < 0)
            return

          begin()

          try {
            const moreResult = await client.book.searchBooks({ bookQuery: { ...convertQuery(query), offset: result.items.length } })

            if (loadId.current < 0)
              return

            if (!moreResult.items.length) {
              loadId.current = -1
              return
            }

            setResult({
              ...moreResult,
              items: [
                ...result.items,
                ...moreResult.items
              ]
            })

            ++loadId.current
          }
          catch (e) {
            notifyError(e)
          }
          finally {
            endProgress()
          }
        }} />
    </animated.div>
  )
}
