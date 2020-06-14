import React, { Dispatch, useContext, useRef, useState, createContext, useMemo, useLayoutEffect } from 'react'
import { useTabTitle } from '../hooks'
import { Prefetch, PrefetchLink, PrefetchLinkProps, usePrefetch } from '../Prefetch'
import { ProgressContext } from '../Progress'
import { useScrollShortcut } from '../shortcuts'
import { GridListing } from './Grid'
import { ClientContext } from '../ClientContext'
import { LayoutContent } from '../Layout'
import { SearchManager, SearchState } from './searchManager'
import { Header } from './Header'
import { LocaleContext } from '../LocaleProvider'

export function getBookListingPrefetch(): Prefetch<SearchState> {
  return {
    path: '/books',

    func: async client => {
      const manager = new SearchManager(client)

      if (client.currentInfo.authenticated) {
        manager.language = client.currentInfo.user.language
      }

      manager.canRefresh = true
      await manager.refresh()

      return manager.state
    }
  }
}

export const BookListing = () => {
  const { result, dispatch } = usePrefetch(getBookListingPrefetch())

  if (result)
    return <Loaded state={result} dispatch={dispatch} />

  return null
}

export const BookListingLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getBookListingPrefetch()} {...props} />

export const BookListingContext = createContext<{ manager: SearchManager }>(undefined as any)

const Loaded = ({ state, dispatch }: { state: SearchState, dispatch: Dispatch<SearchState> }) => {
  useTabTitle('Books')
  useScrollShortcut()

  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { locale, setLocale } = useContext(LocaleContext)

  const manager = useRef(new SearchManager(client)).current
  manager.canRefresh = true

  if (manager.id !== state.id)
    manager.emit('state', state)

  useLayoutEffect(() => {
    const onloading = (loading: boolean) => { if (loading) start(); else stop() }
    const onstate = () => {
      setLocale(manager.language)
      dispatch(manager.state)
    }

    manager.on('loading', onloading)
    manager.on('state', onstate)

    return () => {
      manager.off('loading', onloading)
      manager.off('state', onstate)
    }
  }, [dispatch, locale, manager, setLocale, start, stop])

  const [selected, setSelected] = useState<string>()

  return <BookListingContext.Provider value={useMemo(() => ({ manager }), [manager])}>
    <Header />

    <LayoutContent>
      <GridListing
        selected={selected}
        setSelected={setSelected} />
    </LayoutContent>
  </BookListingContext.Provider>
}
