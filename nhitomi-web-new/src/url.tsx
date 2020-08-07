import { Dispatch, useLayoutEffect, useCallback } from 'react'
import { useUpdate } from 'react-use'
import { parse, stringify } from 'qs'

/** Similar to useState but stores data in the query part of window.location. */
export function useUrlState<T>(mode: 'push' | 'replace' = 'replace', name?: string): [T, Dispatch<T>] {
  const update = useUpdate()
  const state = getSelfOrField(deserializeUrlState<T>(window.location.search), name)

  useLayoutEffect(() => {
    const handler = () => {
      const newState = getSelfOrField(deserializeUrlState<T>(window.location.search), name)

      if (state !== newState)
        update()
    }

    window.addEventListener('popstate', handler)
    return () => window.removeEventListener('popstate', handler)
  }, [name, state, update])

  const setState = useCallback((value: T) => {
    const url = new URL(window.location.href)
    url.search = serializeUrlState(value)

    switch (mode) {
      case 'push':
        window.history.pushState(window.history.state, document.title, url.href)
        break
      case 'replace':
        window.history.replaceState(window.history.state, document.title, url.href)
        break
    }
  }, [mode])

  return [state, setState]
}

function getSelfOrField<T>(value: T, name?: string) {
  if (name && typeof value === 'object')
    return (value as any)[name] as T
  else
    return value
}

function serializeUrlState<T extends {}>(value: T) {
  return stringify(value, {
    addQueryPrefix: true,
    allowDots: true,
    arrayFormat: 'brackets',
    skipNulls: true,
    sort: (a, b) => a.localeCompare(b)
  })
}

function deserializeUrlState<T extends {}>(value: string) {
  return parse(value, {
    ignoreQueryPrefix: true,
    allowDots: true,
    arrayLimit: 100,
    depth: 10
  }) as T
}
