import React, { Dispatch, useCallback, useLayoutEffect, useRef, ComponentProps, useMemo } from 'react'
import { useUpdate, useAsync } from 'react-use'
import { Client, useClient } from './ClientManager'
import { useProgress } from './ProgressManager'
import { Link, LinkProps } from 'wouter-preact'
import { getEventModifiers } from './shortcut'
import { useNotify } from './NotificationManager'

// https://stackoverflow.com/a/53307588/13160620
let refreshed = false

try {
  const entry = performance.getEntriesByType('navigation')[0]

  refreshed = entry instanceof PerformanceNavigationTiming && entry.type === 'reload'
}
catch {
  try { refreshed = performance.navigation.type === 1 }
  catch { /* ignored */ }
}

if (refreshed) {
  // on refresh, clear all page states except scroll to allow refetching (causes usePostfetch to be triggered)
  window.history.replaceState({ scroll: window.history.state?.scroll }, document.title)
}

type PageState<T> = {
  value: T | undefined
  version: number
}

/** Similar to useState but stores the data in window.history.state. */
export function usePageState<T>(key: string): [T | undefined, Dispatch<T | undefined>] {
  const update = useUpdate()
  const state = window.history.state?.[key] as PageState<T> | undefined
  const version = useRef(state?.version || Math.random()) // versioning is used for equality instead of state value, allowing objects to be compared correctly

  useLayoutEffect(() => {
    const handler = (e: PopStateEvent) => {
      const newState = e.state?.[key] as PageState<T> | undefined

      if (newState?.version !== version.current)
        update()

      version.current = newState?.version || Math.random()
    }

    window.addEventListener('popstate', handler)
    return () => window.removeEventListener('popstate', handler)
  }, [key, update])

  const setState = useCallback((value: T | undefined) => {
    window.history.replaceState(getModifiedHistoryState(key, value), document.title)
    update()
  }, [key, update])

  return [state?.value, setState]
}

function getModifiedHistoryState<T>(key: string, value: T): { [key: string]: PageState<unknown> } {
  return { ...window.history.state, [key]: { value, version: Math.random() } }
}

/** Stores window scroll position in the page state for retainment between navigations. */
export const PrefetchScrollPreserver = () => {
  const [, setScroll] = usePageState<number>('scroll')

  const flush = useRef<number>()

  useLayoutEffect(() => {
    const handler = () => {
      clearTimeout(flush.current)
      flush.current = window.setTimeout(() => setScroll(window.scrollY), 100)
    }

    window.addEventListener('scroll', handler)
    return () => window.removeEventListener('scroll', handler)
  }, [setScroll])

  return null
}

export type PrefetchMode = 'prefetch' | 'postfetch'

export type Prefetch<T, U = {}> = {
  path: string

  showProgress?: boolean
  restoreScroll?: boolean

  useData?: (mode: PrefetchMode) => U
  fetch: (client: Client, mode: PrefetchMode, data: U) => Promise<T>
}

/** Returns a function that will fetch data and navigate to a page. Prefetch object should be memoized. */
export function usePrefetch<T>(prefetch: Prefetch<T>) {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()

  // retrieve data from memoized custom hook function
  const data = useRef(prefetch.useData).current?.('prefetch')

  return useCallback(async () => {
    const { path, showProgress, restoreScroll, fetch } = prefetch

    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'prefetch', data || {})

      window.history.pushState(getModifiedHistoryState('data', value), document.title, path)

      if (restoreScroll)
        window.scrollTo({ top: 0 })
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      if (showProgress)
        end()
    }
  }, [prefetch, client, begin, end, notifyError, data])
}

/** Fetches data for the current page if not already fetched. Prefetch object should be memoized. */
export function usePostfetch<T>(prefetch: Prefetch<T>) {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()

  const [state, setState] = usePageState<T>('data')
  const [scroll] = usePageState<number>('scroll')

  // retrieve data from memoized custom hook function
  const data = useRef(prefetch.useData).current?.('postfetch')

  const { error, loading } = useAsync(async () => {
    const { showProgress, restoreScroll, fetch } = prefetch

    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'postfetch', data || {})

      setState(value)

      if (restoreScroll)
        window.scrollTo({ top: scroll })

      return value
    }
    catch (e) {
      notifyError(e)
      throw e
    }
    finally {
      if (showProgress)
        end()
    }
  }, [prefetch]) // refetch only if prefetch object changes

  return {
    result: state,
    dispatch: setState,
    error,
    loading
  }
}

export type PrefetchLinkProps = ComponentProps<typeof PrefetchLink>

/** Link that fetches some data before navigating to a page. */
export const PrefetchLink = <T extends {}>({ fetch, disabled, target, onClick, ...props }: Omit<LinkProps, 'to'> & {
  /** fetch information */
  fetch: Prefetch<T>

  /** if true, prevents navigation when clicking this link. */
  disabled?: boolean
}) => {
  const go = usePrefetch(fetch)

  return (
    <Link
      to={fetch.path}
      onClick={e => {
        onClick?.call(e.target!, e)

        // don't handle modifiers
        if (getEventModifiers(e).length)
          return

        // don't handle blank targets
        if (target === '_blank')
          return

        // prevent default navigation
        e.preventDefault()

        if (disabled)
          return

        go()
      }}
      target={target}
      {...props as any} />
  )
}
