import React, { Dispatch, useMemo, useRef, useLayoutEffect } from 'react'
import { SearchQuery, convertQuery, DefaultQueryLimit, performQuery } from './search'
import { useQueryState, usePageState } from '../state'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { BookSearchResult, BookSort, SortDirection, Book } from 'nhitomi-api'
import { SearchInput } from './SearchInput'
import { BookList } from '../Components/BookList'
import { useNotify } from '../NotificationManager'
import { useClient, useClientInfo } from '../ClientManager'
import { LoadContainer } from '../Components/LoadContainer'
import { useProgress } from '../ProgressManager'
import { useScrollShortcut } from '../shortcut'
import { useConfig } from '../ConfigManager'
import { useSpring, animated } from 'react-spring'
import { PageContainer } from '../Components/PageContainer'
import stringify from 'json-stable-stringify'
import { Container } from '../Components/Container'
import { Menu } from './Menu'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { EmptyIndicator } from '../Components/EmptyIndicator'
import { FormattedMessage } from 'react-intl'
import { useAsync } from 'react-use'

export type PrefetchResult = BookSearchResult & { nextOffset: number }
export type PrefetchOptions = { query?: SearchQuery }

export const useBookListingPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, query: targetQuery }) => {
  const client = useClient()
  const { info } = useClientInfo()
  const [languages] = useConfig('searchLanguages')
  const [currentQuery] = useQueryState<SearchQuery>()

  const query = targetQuery || (mode === 'postfetch' && currentQuery) || {}

  query.query = query.query || ''

  // sort by updated time
  query.sort = query.sort || BookSort.UpdatedTime
  query.order = query.order || SortDirection.Descending

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
      const result = await performQuery(client, info, query)

      return { ...result, nextOffset: DefaultQueryLimit }
    }
  }
}

export const BookListingLink = ({ query, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useBookListingPrefetch} options={{ query }} {...props} />
)

export const BookListing = (options: PrefetchOptions) => {
  const { result, setResult } = usePostfetch(useBookListingPrefetch, { requireAuth: true, ...options })

  useScrollShortcut()

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded result={result} setResult={setResult} />
    </PageContainer>
  )
}

const Loaded = ({ result, setResult }: { result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  const [query] = useQueryState<SearchQuery>()
  const queryId = useRef(0)

  useTabTitle(query.query, useLocalized('pages.bookListing.title'))

  const client = useClient()
  const { info } = useClientInfo()
  const { notifyError } = useNotify()
  const { begin, end } = useProgress()

  // displayed results may not represent the current query if we navigated before storing the results
  const [effectiveQuery, setEffectiveQuery] = usePageState<SearchQuery>('query', query)

  // serialized query string is used for comparison
  const queryCmp = useMemo(() => stringify(query), [query])
  const effectiveQueryCmp = useMemo(() => stringify(effectiveQuery || {}), [effectiveQuery])

  // perform search when query changes
  useAsync(async () => {
    if (queryCmp === effectiveQueryCmp)
      return

    begin()

    const id = ++queryId.current

    try {
      const result = await performQuery(client, info, query)

      if (queryId.current === id) {
        setResult({ ...result, nextOffset: DefaultQueryLimit })
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

  return (
    <Container>
      <Input result={result} />

      <BookList
        items={result.items}
        menu={(
          <Menu />
        )}
        empty={(
          <EmptyIndicator>
            <FormattedMessage id='pages.bookListing.empty' />
          </EmptyIndicator>
        )} />

      <Loader key={effectiveQueryCmp} query={query} result={result} setResult={setResult} />
    </Container>
  )
}

const Input = ({ result }: { result: PrefetchResult }) => {
  const style = useSpring({
    from: { opacity: 0, marginTop: -5 },
    to: { opacity: 1, marginTop: 0 }
  })

  return (
    <div className='mx-auto p-4 w-full max-w-2xl sticky top-0 z-20'>
      <animated.div style={style} className='w-full'>
        <SearchInput result={result} className='shadow-lg w-full' />
      </animated.div>
    </div>
  )
}

const Loader = ({ query, result, setResult }: { query: SearchQuery, result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end: endProgress } = useProgress()

  const loadId = useRef(result.nextOffset >= result.total ? -1 : 0)

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
            const moreResult = await client.book.searchBooks({ bookQuery: { ...convertQuery(query), offset: result.nextOffset } })

            if (loadId.current < 0)
              return

            if (!moreResult.items.length) {
              loadId.current = -1
              return
            }

            // remove duplicates
            const items: Book[] = []
            const exists: { [id: string]: true } = {}

            for (const item of [...result.items, ...moreResult.items]) {
              if (!exists[item.id])
                items.push(item)

              exists[item.id] = true
            }

            setResult({
              ...result,
              ...moreResult,

              items,
              nextOffset: result.nextOffset + DefaultQueryLimit
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
