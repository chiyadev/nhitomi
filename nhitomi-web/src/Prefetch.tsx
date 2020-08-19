import React, { useCallback, useLayoutEffect, useRef, ComponentProps, ReactNode } from 'react'
import { useAsync } from './hooks'
import { useProgress } from './ProgressManager'
import { getEventModifiers } from './shortcut'
import { useNotify } from './NotificationManager'
import { Link, LinkProps } from 'react-router-dom'
import { usePageState, NavigationArgs, useNavigator, NavigationMode } from './state'
import { cx } from 'emotion'
import { useClientInfo } from './ClientManager'
import { useAuthenticationPrefetch } from './Authentication'

function beginScrollTo(scroll: number, retry = 0) {
  requestAnimationFrame(() => {
    window.scrollTo({ top: scroll })

    // components may not render immediately after prefetch
    if (retry < 5)
      beginScrollTo(scroll, retry + 1)
  })
}

export type PrefetchMode = 'prefetch' | 'postfetch'

export type PrefetchGenerator<T, U extends {} = {}> = (x: U & { mode: PrefetchMode }) => {
  destination: NavigationArgs

  showProgress?: boolean
  restoreScroll?: boolean

  /** Fetches the data. */
  fetch: () => Promise<T>

  /** Called after the fetch succeeded and the page navigated. */
  done?: (fetched: T) => Promise<void> | void
}

/** Returns a function that will fetch data and navigate to a page. */
export function usePrefetch<T, U extends {}>(generator: PrefetchGenerator<T, U>, options: U): [string, (mode?: NavigationMode) => Promise<T | undefined>] {
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const navigator = useNavigator()

  // generator can call hooks so memoize it
  const { destination, showProgress = true, restoreScroll = true, fetch, done } = useRef(generator).current(({ mode: 'prefetch', ...options }))

  // unlike postfetch, prefetch moves to another location so treat unspecified query and hash as empty
  destination.query = destination.query || {}
  destination.hash = destination.hash || ''

  const run = useCallback(async (mode: NavigationMode = 'push') => {
    const startingPath = navigator.path

    if (showProgress)
      begin()

    try {
      const fetched = await fetch()
      const location = navigator.evaluate(destination)

      // abort if navigated during fetch
      if (navigator.path !== startingPath)
        return

      navigator.navigate(mode, {
        ...location,
        state: {
          ...(mode === 'push' ? {} : location.state), // clear previous states if pushing
          scroll: { value: restoreScroll ? 0 : location.state.scroll?.value, version: Math.random() },
          fetch: { value: fetched, version: Math.random() }
        }
      })

      if (restoreScroll)
        beginScrollTo(0)

      await done?.(fetched)

      return fetched
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      end()
    }
  }, [begin, destination, done, end, fetch, navigator, notifyError, restoreScroll, showProgress])

  return [navigator.stringify(navigator.evaluate(destination)), run]
}

/** Fetches data for the current page if not already fetched. */
export function usePostfetch<T, U extends {}>(generator: PrefetchGenerator<T, U>, options: U & { requireAuth?: boolean }) {
  const { info } = useClientInfo()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const navigator = useNavigator()

  const [result, setResult] = usePageState<T>('fetch')
  const [scroll] = usePageState<number>('scroll')

  const shouldScroll = useRef(true)

  // generator can call hooks so memoize it
  const { destination, showProgress = true, restoreScroll = true, fetch, done } = useRef(generator).current(({ mode: 'postfetch', ...options }))
  const { requireAuth } = options

  const [, navigateAuth] = usePrefetch(useAuthenticationPrefetch, { redirect: destination })

  const { error, loading } = useAsync(async () => {
    // redirect to auth if not already authenticated
    if (requireAuth && !info.authenticated) {
      await navigateAuth('replace')
      return
    }

    // display immediately if already loaded
    if (result) {
      if (shouldScroll.current && restoreScroll && typeof scroll === 'number') {
        beginScrollTo(scroll)
        shouldScroll.current = false
      }

      return
    }

    const startingPath = navigator.path

    if (showProgress)
      begin()

    try {
      const fetched = await fetch()
      const location = navigator.evaluate(destination)

      // abort if navigated during fetch
      if (navigator.path !== startingPath)
        return

      navigator.navigate('replace', {
        ...location,
        state: {
          ...location.state,
          scroll: { value: restoreScroll && typeof scroll === 'number' ? scroll : location.state.scroll?.value, version: Math.random() },
          fetch: { value: fetched, version: Math.random() }
        }
      })

      if (restoreScroll && typeof scroll === 'number') {
        beginScrollTo(scroll)
        shouldScroll.current = false
      }

      await done?.(fetched)
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      if (showProgress)
        end()
    }
  }, [result, navigator.stringify(navigator.evaluate(destination))])

  return { result, setResult, error, loading }
}

export type PrefetchLinkProps = ComponentProps<typeof PrefetchLink>
export type TypedPrefetchLinkProps = Omit<ComponentProps<typeof PrefetchLink>, 'fetch' | 'options'>

/** Link that fetches some data before navigating to a page. */
export const PrefetchLink = <T extends any, U extends {} = {}>({ fetch, options, mode, className, target, onClick, ...props }: Omit<LinkProps, 'to' | 'href'> & {
  fetch: PrefetchGenerator<T, U>
  options: U
  mode?: NavigationMode
}) => {
  const [destination, run] = usePrefetch(fetch, options)

  return (
    <Link
      to={destination}
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

        run(mode)
      }}
      target={target}

      {...props} />
  )
}

export const BackLink = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const { history } = useNavigator()

  return (
    <div
      className={cx('display-contents', className)}
      children={children}
      onClick={() => history.goBack()} />
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
