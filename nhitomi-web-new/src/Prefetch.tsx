import React, { Dispatch, useCallback, useLayoutEffect, useRef, ComponentProps } from 'react'
import { useUpdate, useAsync } from 'react-use'
import { Client, useClient } from './ClientManager'
import { useProgress } from './ProgressManager'
import { getEventModifiers } from './shortcut'
import { useNotify } from './NotificationManager'
import { Link, LinkProps } from 'react-router-dom'
import { History, navigate } from './history'

/** Similar to useState but stores the data in window.history.state. */
export function usePageState<T>(key: string): [T | undefined, Dispatch<T | undefined>] {
  const update = useUpdate()
  const state = History.location.state?.[key]
  const version = useRef(state?.version || Math.random()) // versioning is used for equality instead of state value, allowing objects to be compared correctly
  const validPath = useRef(History.location.pathname)

  useLayoutEffect(() => History.listen(location => {
    if (location.pathname !== validPath.current)
      return

    const newState = location.state?.[key]

    if (newState?.version !== version.current)
      update()

    version.current = newState?.version || Math.random()
  }), [key, update])

  const setState = useCallback((value: T | undefined) => {
    navigate('replace', { state: s => ({ ...s, [key]: { value, version: Math.random() } }) })
  }, [key])

  return [state?.value as T, setState]
}

function beginScrollTo(scroll: number, retry = 0) {
  requestAnimationFrame(() => {
    window.scrollTo({ top: scroll })

    // components may not render immediately after prefetch, so retry scroll if destination not reached
    if (Math.floor(window.scrollY) !== Math.floor(scroll) && retry < 30)
      beginScrollTo(scroll, retry + 1)
  })
}

export type PrefetchMode = 'prefetch' | 'postfetch'

export type Prefetch<T, U = {}> = {
  /** Path to navigate to after fetch. */
  path: string

  showProgress?: boolean
  restoreScroll?: boolean

  /** Calls hooks and passes the result to the fetch function. */
  useData?: (mode: PrefetchMode) => U

  /** Fetches the data. */
  fetch: (client: Client, mode: PrefetchMode, data: U) => Promise<T>

  /** Called after the fetch succeeded and the page navigated. */
  done?: (fetched: T, client: Client, mode: PrefetchMode, data: U) => Promise<void> | void
}

/** Returns a function that will fetch data and navigate to a page. Prefetch object should be memoized. */
export function usePrefetch<T>(prefetch: Prefetch<T, any>) {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()

  // retrieve data from memoized custom hook function
  const data = useRef(prefetch.useData).current?.('prefetch')

  return useCallback(async () => {
    const { path, showProgress = true, restoreScroll = true, fetch, done } = prefetch

    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'prefetch', data || {})

      navigate('push', {
        path, search: '', hash: '', state: s => ({
          ...s,
          scroll: { value: restoreScroll ? 0 : (s.scroll?.value || 0), version: Math.random() }, // restoreScroll for prefetch is top
          fetch: { value, version: Math.random() }
        })
      })

      if (restoreScroll)
        beginScrollTo(0)

      await done?.(value, client, 'prefetch', data || {})
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
export function usePostfetch<T>(prefetch: Prefetch<T, any>) {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()

  const [state, setState] = usePageState<T>('fetch')
  const [scroll] = usePageState<number>('scroll')

  // retrieve data from memoized custom hook function
  const data = useRef(prefetch.useData).current?.('postfetch')

  const { error, loading } = useAsync(async () => {
    const { showProgress = true, restoreScroll = true, fetch, done } = prefetch

    // display immediately if already loaded
    if (state) {
      if (restoreScroll && scroll)
        beginScrollTo(scroll)

      return
    }

    if (showProgress)
      begin()

    try {
      const value = await fetch(client, 'postfetch', data || {})

      setState(value)

      if (restoreScroll && scroll)
        beginScrollTo(scroll)

      await done?.(value, client, 'postfetch', data || {})

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
export const PrefetchLink = <T extends {}>({ fetch, disabled, target, onClick, ...props }: Omit<LinkProps, 'to' | 'href'> & {
  /** fetch information */
  fetch: Prefetch<T, any>

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
