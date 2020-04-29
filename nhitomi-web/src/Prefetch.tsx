import React, { ComponentProps, Dispatch, useCallback, useContext, useEffect } from 'react'
import { Link, useHistory } from 'react-router-dom'
import { useAsync, useUpdate } from 'react-use'
import { Client } from './Client'
import { eventHasAnyModifier } from './shortcuts'
import { ClientContext } from './ClientContext'
import { ProgressContext } from './Progress'
import { NotificationContext } from './NotificationContext'

// "Prefetch" is a convenient utility that allows data to be fetched on clicking a link, without transitioning to the target page immediately.
// Fetched data is saved to window's history entry, becoming available to the page component through usePageState hook.
// This is used to immitate traditional browsers, like showing a loading progress bar at the top of the page while fetching, and retaining page data across navigations.
// By using prefetch, we do not have to render ugly and meaningless placeholders.

export type Prefetch<T> = {
  /** path to navigate to after prefetch */
  path: string

  /** true to show progress while fetching */
  progress?: boolean

  /** true to scroll window to top after navigation */
  scroll?: boolean

  /** function to do fetching */
  func: (client: Client) => Promise<T>
}

type HistoryLocationState = { [key: string]: unknown }

/** Similar to useState but backed by history.state. */
export function usePageState<T>(name: string): [T | undefined, Dispatch<T | undefined>]

/** Similar to useState but backed by history.state. */
export function usePageState<T>(name: string, defaultValue: T): [T, Dispatch<T>]

export function usePageState(name: string, defaultValue?: any) {
  const rerender = useUpdate()
  const { location, replace, listen } = useHistory<HistoryLocationState>()

  // rerender on state change
  useEffect(() => listen(rerender), [listen, rerender])

  const value = (location.state || {})[name] || defaultValue
  const setValue = useCallback((v: any) => replace({ ...location, state: { ...location.state, [name]: v } }), [location, name, replace])

  return [value, setValue]
}

/**
 * Fetches if not already fetched.
 * Result can be retrieved directly from the return value or from usePageState.
 */
export function usePrefetch<T>({ path, progress = true, scroll = true, func: prefetch }: Prefetch<T>) {
  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { notification } = useContext(NotificationContext)

  const [state, setState] = usePageState<T>(path)

  const { error, loading } = useAsync(async () => {
    // already loaded
    if (state)
      return

    if (progress)
      start()

    try {
      const value = await prefetch(client)

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

  return {
    result: state,
    dispatch: setState,
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

  const { push } = useHistory<HistoryLocationState>()

  return async <T extends {}>({ path, progress = true, scroll = true, func: prefetch }: Prefetch<T>) => {
    if (progress)
      start()

    try {
      const value = await prefetch(client)

      push(path, { [path]: value })

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
  const { location, replace } = useHistory<HistoryLocationState>()

  return () => replace({ ...location, state: {} })
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
      // don't handle modifiers
      if (eventHasAnyModifier(e))
        return

      // prevent default navigation
      e.preventDefault()

      if (disabled)
        return

      onClick?.(e)
      executor(fetch)
    }}
    {...props} />
}
