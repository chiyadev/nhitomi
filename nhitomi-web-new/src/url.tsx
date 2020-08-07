import { Dispatch, useLayoutEffect, useCallback } from 'react'
import { useUpdate } from 'react-use'
import { parse, stringify } from 'qs'
import { History, navigate, NavigationMode } from './history'

/** Similar to useState but stores data in the query part of window.location. */
export function useUrlState<T extends (N extends string ? any : object), N extends string | undefined = undefined>(mode: NavigationMode = 'replace', name?: N): [T | undefined, Dispatch<T | undefined>] {
  const update = useUpdate()
  const state = getSelfOrField<T>(deserialize(History.location.search), name)

  useLayoutEffect(() => History.listen(location => {
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

  return [state, setState]
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
