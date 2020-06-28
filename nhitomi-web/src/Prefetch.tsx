import React, { ComponentProps, Dispatch, useCallback, useContext, useLayoutEffect, useRef } from 'react'
import { History } from 'history'
import { Link, useHistory } from 'react-router-dom'
import { useAsync, useUpdate } from 'react-use'
import { Client } from './Client'
import { getEventModifiers } from './shortcuts'
import { ClientContext } from './ClientContext'
import { ProgressContext } from './Progress'
import { NotificationContext } from './NotificationContext'

// "Prefetch" is a convenient utility that allows data to be fetched on clicking a link, without transitioning to the target page immediately.
// Fetched data is saved to window's history entry, becoming available to the page component through usePageState hook.
// This is used to imitate traditional browsers, like showing a loading progress bar at the top of the page while fetching, and retaining page data across navigations.
// By using prefetch, we can avoid having to render ugly placeholders or transitioning pages.

export type PrefetchMode = 'initial' | 'navigate'

export type Prefetch<T> = {
  /** path to navigate to after prefetch */
  path: string

  /** path to update navigation to after dispatch */
  getPath?: (fetched: T) => string

  /** true to show progress while fetching */
  progress?: boolean

  /** true to scroll window to top after navigation */
  scroll?: boolean

  /** function to do fetching */
  func: (client: Client, mode: PrefetchMode, history: History<HistoryState>) => Promise<T>
}

type HistoryState = { [key: string]: unknown }

/** Similar to useState but backed by history.state. */
export function usePageState<T>(): [T | undefined, Dispatch<T | undefined>] {
  const rerender = useUpdate()
  const history = useHistory<HistoryState>()

  useLayoutEffect(() => history.listen(rerender), [history, rerender])

  // cache path in closure so that it remains consistent across navigations
  const path = useRef(history.location.pathname).current

  const value = history.location.state?.[path] as T | undefined
  const setValue = useCallback((v: T | undefined) => history.replace({ ...history.location, state: { ...history.location.state, [path]: v } }), [history, path])
  const lastValue = useRef(value)

  return [lastValue.current = value || lastValue.current, setValue]
}

/**
 * Fetches if not already fetched.
 * Result can be retrieved directly from the return value or from usePageState.
 */
export function usePrefetch<T>({ getPath, progress = true, scroll = true, func: prefetch }: Prefetch<T>) {
  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { notification } = useContext(NotificationContext)

  const history = useHistory<HistoryState>()
  const [state, setState] = usePageState<T>()

  const { error, loading } = useAsync(async () => {
    // already loaded
    if (state)
      return

    if (progress)
      start()

    try {
      const value = await prefetch(client, 'initial', history)

      setState(value)

      if (scroll)
        window.scrollTo({ top: 0 })

      return value
    }
    catch (e) {
      notification.error(e)
      throw e
    }
    finally {
      if (progress)
        stop()
    }
  }, [state])

  const dispatch = useCallback((fetched: T) => {
    const path = getPath?.(fetched)

    // path recalculation based on state
    if (path && path !== history.location.pathname)
      history.replace({ ...history.location, pathname: path })

    setState(fetched)
  }, [getPath, history, setState])

  return {
    result: state,
    dispatch,
    error,
    loading
  }
}

/**
 * Returns a function that will fetch some data before navigating to a page.
 * This is the imperative equivalent of PrefetchLink.
 */
export function usePrefetchExecutor() {
  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { notification } = useContext(NotificationContext)

  const history = useHistory<HistoryState>()

  return async <T extends {}>({ path, progress = true, scroll = true, func: prefetch }: Prefetch<T>) => {
    if (progress)
      start()

    try {
      const value = await prefetch(client, 'navigate', history)

      history.push(path, { [path]: value })

      if (scroll)
        window.scrollTo({ top: 0 })
    }
    catch (e) {
      notification.error(e)
    }
    finally {
      if (progress)
        stop()
    }
  }
}

/** Returns a function that resets the prefetched data for the current page. */
export function usePrefetchReset() {
  const history = useHistory<HistoryState>()

  return useCallback(() => history.replace({ ...history.location, state: { ...history.location.state, [history.location.pathname]: undefined } }), [history])
}

export type PrefetchLinkProps = Omit<ComponentProps<typeof PrefetchLink>, 'fetch'>

/** Link that fetches some data before navigating to a page. */
export const PrefetchLink = <T extends {}>({ fetch, disabled, onClick, ...props }: Omit<ComponentProps<typeof Link>, 'to'> & {
  /** fetch information */
  fetch: Prefetch<T>

  /** if true, prevents navigation when clicking this link. */
  disabled?: boolean
}) => {
  const executor = usePrefetchExecutor()

  return <Link
    to={fetch.path}
    onClick={e => {
      onClick?.(e)

      // don't handle modifiers
      if (getEventModifiers(e).length)
        return

      // prevent default navigation
      e.preventDefault()

      if (disabled)
        return

      executor(fetch)
    }}
    {...props} />
}
