import React, { Dispatch, useCallback, useLayoutEffect, useRef, useContext, ComponentProps } from 'react'
import { useUpdate, useAsync } from 'react-use'
import { Client, ClientContext } from './ClientManager'
import { ProgressContext } from './ProgressManager'
import { Link, LinkProps } from 'wouter-preact'
import { getEventModifiers } from './shortcut'

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

  const setValue = useCallback((value: T | undefined) => {
    window.history.replaceState(getModifiedHistoryState(key, value), document.title)
    update()
  }, [key, update])

  return [state?.value, setValue]
}

export function getModifiedHistoryState<T>(key: string, value: T): { [key: string]: PageState<unknown> } {
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

export type Prefetch<T> = {
  path: string

  showProgress?: boolean
  restoreScroll?: boolean

  fetch: (client: Client, mode: PrefetchMode) => Promise<T>
}

/** Returns a function that will fetch data and navigate to a page. */
export function usePrefetch() {
  const { client } = useContext(ClientContext)
  const { begin, end } = useContext(ProgressContext)
  // const { notification } = useContext(NotificationContext)

  return async <T extends {}>({ path, showProgress = true, restoreScroll = true, fetch }: Prefetch<T>) => {
    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'prefetch')

      window.history.pushState(getModifiedHistoryState('data', value), document.title, path)

      if (restoreScroll)
        window.scrollTo({ top: 0 })
    }
    // catch (e) {
    //   notification.error(e)
    // }
    finally {
      if (showProgress)
        end()
    }
  }
}

/** Fetches data for the current page if not already fetched. Prefetch object should be memoized. */
export function usePostfetch<T>(prefetch: Prefetch<T>) {
  const { client } = useContext(ClientContext)
  const { begin, end } = useContext(ProgressContext)
  // const { notification } = useContext(NotificationContext)

  const [state, setState] = usePageState<T>('data')
  const [scroll] = usePageState<number>('scroll')

  const { error, loading } = useAsync(async () => {
    const { showProgress, restoreScroll, fetch } = prefetch

    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'postfetch')

      setState(value)

      if (restoreScroll)
        window.scrollTo({ top: scroll })

      return value
    }
    // catch (e) {
    //   notification.error(e)
    //   throw e
    // }
    finally {
      if (showProgress)
        end()
    }
  }, [prefetch])

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
  const go = usePrefetch()

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

        go(fetch)
      }}
      target={target}
      {...props as any} />
  )
}
