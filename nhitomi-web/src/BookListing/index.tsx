import React, { Dispatch, useContext, useRef, createContext, useMemo, useLayoutEffect } from 'react'
import { useTabTitle } from '../hooks'
import { Prefetch, PrefetchLink, PrefetchLinkProps, usePrefetch } from '../Prefetch'
import { ProgressContext } from '../Progress'
import { useScrollShortcut } from '../shortcuts'
import { Grid } from './Grid'
import { ClientContext } from '../ClientContext'
import { LayoutContent } from '../Layout'
import { SearchManager, SearchQuery, SearchResult, serializeQuery, deserializeQuery } from './searchManager'
import { Header } from './Header'
import { LocaleContext } from '../LocaleProvider'
import { NotificationContext } from '../NotificationContext'
import { useHistory } from 'react-router-dom'
import { useIntl } from 'react-intl'

export function getBookListingPrefetch(): Prefetch<SearchResult> {
  return {
    path: '/books',

    func: async (client, mode, { location: { search } }) => {
      const manager = new SearchManager(client)
      manager.canRefresh = false

      if (client.currentInfo.authenticated) {
        manager.query = {
          ...manager.query,
          language: client.currentInfo.user.language
        }
      }

      if (mode === 'initial' && search)
        manager.query = deserializeQuery(search)

      manager.canRefresh = true
      return await manager.refresh()
    }
  }
}

export const BookListing = () => {
  const { result, dispatch } = usePrefetch(getBookListingPrefetch())

  if (result)
    return <Loaded result={result} dispatch={dispatch} />

  return null
}

export const BookListingLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getBookListingPrefetch()} {...props} />

export const BookListingContext = createContext<{ manager: SearchManager }>(undefined as any)

const Loaded = ({ result, dispatch }: { result: SearchResult, dispatch: Dispatch<SearchResult> }) => {
  const { formatMessage } = useIntl()

  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { locale, setLocale } = useContext(LocaleContext)
  const { notification: { error } } = useContext(NotificationContext)
  const { location, push, replace } = useHistory()

  const manager = useRef(new SearchManager(client)).current

  useScrollShortcut()
  useTabTitle(manager.getReadableQuery(), formatMessage({ id: 'bookListing.header.title' }))

  useLayoutEffect(() => {
    const onloading = (loading: boolean) => { if (loading) start(); else stop() }
    const onquery = (query: SearchQuery, shouldPush: boolean) => {
      setLocale(query.language);

      (shouldPush ? push : replace)({
        ...location,
        search: serializeQuery(query)
      })
    }

    manager.on('loading', onloading)
    manager.on('query', onquery)
    manager.on('result', dispatch)
    manager.on('failed', error)

    return () => {
      manager.off('loading', onloading)
      manager.off('query', onquery)
      manager.off('result', dispatch)
      manager.off('failed', error)
    }
  }, [dispatch, error, locale, location, manager, push, replace, setLocale, start, stop])

  // must come after event attachments
  useLayoutEffect(() => {
    manager.replace(location.search ? deserializeQuery(location.search) : result.query, result)
  }, [location.search, manager, result])

  return <BookListingContext.Provider value={useMemo(() => ({ manager }), [manager])}>
    <Header />

    <LayoutContent>
      <Grid />
    </LayoutContent>
  </BookListingContext.Provider>
}
