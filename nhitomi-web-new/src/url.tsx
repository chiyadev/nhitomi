import { Dispatch, useLayoutEffect, useCallback, useRef } from 'react'
import { useUpdate } from 'react-use'
import { parse, stringify } from 'qs'
import { History, navigate, NavigationMode } from './history'

export function useUrlState<T extends {}>(mode?: NavigationMode): [T, Dispatch<T>]
export function useUrlState<T>(mode?: NavigationMode, name?: string): [T | undefined, Dispatch<T | undefined>]

/** Similar to useState but stores data in the query part of window.location. */
export function useUrlState<T>(mode: NavigationMode = 'replace', name?: string): [T, Dispatch<T>] {
  const update = useUpdate()
  const state = getSelfOrField<T>(deserialize(History.location.search), name)
  const validPath = useRef(History.location.pathname)

  useLayoutEffect(() => History.listen(location => {
    if (location.pathname !== validPath.current)
      return

    const newState = getSelfOrField<T>(deserialize(location.search), name)

    if (state !== newState)
      update()
  }), [name, state, update])

  const setState = useCallback((value: T | undefined) => {
    // combine this state with other url states depending on whether name is specified
    let combined: any

    if (name) {
      combined = { ...deserialize(History.location.search), [name!]: value }
    } else {
      combined = { ...deserialize(History.location.search), ...value }
    }

    navigate(mode, { search: serialize(combined) })
  }, [mode, name])

  return [state as any, setState]
}

function serialize(data: unknown) {
  return stringify(data, {
    addQueryPrefix: true,
    allowDots: true,
    arrayFormat: 'brackets',
    skipNulls: true,
    sort: (a, b) => a.localeCompare(b)
  })
}

function deserialize(value: string) {
  return parse(value, {
    ignoreQueryPrefix: true,
    allowDots: true,
    arrayLimit: 100,
    depth: 10
  }) as any
}

function getSelfOrField<T>(value: any, key?: string): T | undefined {
  if (key) return value?.[key]
  else return value
}
