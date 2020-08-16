import { DependencyList, useLayoutEffect, useState } from 'react'
import { useAsyncFn } from 'react-use'
import { FnReturningPromise } from 'react-use/lib/util'

// these are react-use hooks reimplemented using useLayoutEffect instead of useEffect

export function useAsync<T extends FnReturningPromise>(fn: T, deps: DependencyList = []) {
  const [state, callback] = useAsyncFn(fn, deps, {
    loading: true
  })

  useLayoutEffect(() => { callback() }, [callback])

  return state
}

export function useWindowSize() {
  const [state, setState] = useState<{
    width: number
    height: number
  }>({
    width: window.innerWidth,
    height: window.innerHeight
  })

  useLayoutEffect(() => {
    const handler = () => setState({
      width: window.innerWidth,
      height: window.innerHeight
    })

    window.addEventListener('resize', handler)
    return () => { window.removeEventListener('resize', handler) }
  }, [])

  return state
}

export function useWindowScroll() {
  const [state, setState] = useState<{
    x: number
    y: number
  }>({
    x: window.scrollX,
    y: window.scrollY
  })

  useLayoutEffect(() => {
    const handler = () => setState({
      x: window.scrollX,
      y: window.scrollY
    })

    window.addEventListener('scroll', handler)
    return () => { window.removeEventListener('scroll', handler) }
  }, [])

  return state
}
