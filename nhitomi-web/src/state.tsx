import { Dispatch, useLayoutEffect, useCallback, useState, useRef } from 'react'
import { createBrowserHistory, History as Hisotry, Hash, Pathname, Location, Search } from 'history'
import { parse, stringify } from 'qs'
import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'
import { useUpdate } from 'react-use'

export type NavigationMode = 'push' | 'replace'
export type NavigationLocation = {
  path: Pathname
  query: QueryState
  hash: HashState
  state: HistoryState
}
export type NavigationArgs = Partial<{
  [key in keyof NavigationLocation]: NavigationLocation[key] | ((value: NavigationLocation[key]) => NavigationLocation[key])
}>

export type HistoryState = Record<string, { value: unknown, version: number } | undefined>
export type QueryState = Record<string, number | string | object | null | undefined>
export type HashState = Hash // consistency

export const Navigator: NavigationLocation & {
  /** History object responsible for managing all internal navigations. */
  history: Hisotry<HistoryState | null>

  events: StrictEventEmitter<EventEmitter, {
    navigated: (location: NavigationLocation) => void
  }>

  navigate: (mode: NavigationMode, args: NavigationArgs) => void
  evaluate: (location: NavigationArgs) => NavigationLocation
  stringify: (location: Omit<NavigationLocation, 'state'>) => string
} = {
  history: createBrowserHistory() as any,
  events: new EventEmitter(),

  path: '',
  query: {},
  hash: '',
  state: {},

  navigate: (mode, location) => {
    let run: typeof Navigator['history']['push']

    switch (mode) {
      case 'push': run = Navigator.history.push.bind(Navigator.history); break
      case 'replace': run = Navigator.history.replace.bind(Navigator.history); break
    }

    const { path, query, hash, state } = Navigator.evaluate(location)

    // console.log('navigate', mode, path, query, hash, state)

    run({ pathname: path, search: serializeQuery(query), hash, state })
  },

  evaluate: ({ path, query, hash, state }) => {
    if (typeof path === 'function') path = path(Navigator.path)
    if (typeof path === 'undefined') path = Navigator.path

    if (typeof query === 'function') query = query(Navigator.query)
    if (typeof query === 'undefined') query = Navigator.query

    if (typeof hash === 'function') hash = hash(Navigator.hash)
    if (typeof hash === 'undefined') hash = Navigator.hash

    if (typeof state === 'function') state = state(Navigator.state)
    if (typeof state === 'undefined') state = Navigator.state

    return { path, state, hash, query }
  },

  stringify: ({ path, query, hash }) => {
    return path + serializeQuery(query) + hash
  }
}

Navigator.events.setMaxListeners(0)

function serializeQuery(query: QueryState): Search {
  return stringify(query, {
    addQueryPrefix: true,
    allowDots: true,
    arrayFormat: 'brackets',
    skipNulls: true,
    sort: (a, b) => a.localeCompare(b)
  })
}

function deserializeQuery(query: Search): QueryState {
  return parse(query, {
    ignoreQueryPrefix: true,
    allowDots: true,
    arrayLimit: 100,
    depth: 10
  }) as any
}

function updateNavigator(location: Location<HistoryState | null>) {
  Navigator.path = location.pathname
  Navigator.query = deserializeQuery(location.search)
  Navigator.hash = location.hash
  Navigator.state = location.state || {}

  Navigator.events.emit('navigated', Navigator)
}

updateNavigator(Navigator.history.location)
Navigator.history.listen(updateNavigator)

let refreshed = false

try {
  // https://stackoverflow.com/a/53307588/13160620
  const entry = performance.getEntriesByType('navigation')[0]

  refreshed = entry instanceof PerformanceNavigationTiming && entry.type === 'reload'
}
catch {
  try { refreshed = performance.navigation.type === 1 }
  catch { /* ignored */ }
}

if (refreshed) {
  // clear all stale states on refresh
  Navigator.navigate('replace', { state: {} })
}

export function useNavigator() {
  const update = useUpdate()

  // always return the same navigator but rerender when location changes
  useNavigated(update)

  return Navigator
}

export function useNavigated(handler: (location: NavigationLocation) => void) {
  useLayoutEffect(() => {
    Navigator.events.on('navigated', handler)
    return () => { Navigator.events.off('navigated', handler) }
  }, [handler])
}

/** Similar to useState but stores the data in window.history.state. */
export function usePageState<T>(key: string): [T | undefined, Dispatch<T | undefined>]
export function usePageState<T>(key: string, initialState: T): [T, Dispatch<T>]

export function usePageState(key: string, initialState?: any) {
  if (typeof initialState !== 'undefined' && typeof Navigator.state[key]?.value === 'undefined') {
    Navigator.navigate('replace', { state: s => ({ ...s, [key]: { value: initialState, version: Math.random() } }) })
  }

  const [state, setState] = useState(Navigator.state[key])

  useNavigated(useCallback(location => {
    const newState = location.state?.[key]

    if (newState?.version !== state?.version)
      setState(newState)
  }, [key, state]))

  const mounted = useRef(true)
  useLayoutEffect(() => () => { mounted.current = false }, [])

  return [
    state?.value,
    useCallback((value: any) => { mounted.current && Navigator.navigate('replace', { state: s => ({ ...s, [key]: { value, version: Math.random() } }) }) }, [key])
  ]
}

/** Deserializes the query part of the current url and returns a function that can update it. */
export function useQuery(mode: NavigationMode = 'replace'): [QueryState, Dispatch<QueryState>] {
  const [state, setState] = useState(Navigator.query)

  useNavigated(useCallback(location => {
    const newState = location.query

    setState(newState)
  }, []))

  const mounted = useRef(true)
  useLayoutEffect(() => () => { mounted.current = false }, [])

  return [
    state,
    useCallback((value: QueryState) => { mounted.current && Navigator.navigate(mode, { query: value }) }, [mode])
  ]
}

/** Similar to useState but stores data in the query part of window.location. */
export function useQueryState<T extends {}>(mode?: NavigationMode): [T, Dispatch<T>]
export function useQueryState<T>(mode?: NavigationMode, name?: string): [T | undefined, Dispatch<T | undefined>]

export function useQueryState(mode: NavigationMode = 'replace', name?: string) {
  const [query, setQuery] = useQuery(mode)

  const state = name ? query[name] : query

  const setState = useCallback((value: any) => {
    if (name) // always use the latest query value so that changes from other url states aren't overwritten
      setQuery({ ...Navigator.query, [name]: value })
    else
      setQuery(value)
  }, [name, setQuery])

  return [state, setState]
}
