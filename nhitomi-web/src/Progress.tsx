import { presetPrimaryColors } from '@ant-design/colors'
import nprogress from 'nprogress'
import React, { createContext, ReactNode, useLayoutEffect, useMemo, useRef, useState, useCallback } from 'react'
import { useUpdate } from 'react-use'

import './Progress.css'

export const ProgressContext = createContext<{
  processes: number

  start: () => void
  stop: () => void
  clear: () => void

  color: string
  setColor: (color: string) => void
}>(undefined as any)

/** Manages the top progress bar and provides a context object to all descendants. */
export const ProgressBarProvider = ({ children }: { children?: ReactNode }) => {
  const count = useRef(0) // use ref because start/stop can be called asynchronously

  const [color, setColor] = useState('white')

  // nprogress init
  useLayoutEffect(() => {
    console.log('nprogress reconfiguring')

    const template = (c: string) => `
      <div class="bar" role="bar" style="background: ${c};">
        <div class="peg" style="box-shadow: 0 0 10px ${c}, 0 0 5px ${c};"></div>
      </div>
      <div class="spinner" role="spinner">
        <div class="spinner-icon" style="border-top-color: ${c}; border-left-color: ${c};"></div>
      </div>`

    nprogress.remove()
    nprogress.configure({
      template: template(presetPrimaryColors[color] || color)
    })
    nprogress.render()

    if (count.current)
      nprogress.start()
  }, [color]) // eslint-disable-line

  // use timeout to prevent flickering when fetching multiple resources
  const doneTimeout = useRef<number>()

  // nprogress start/done
  useLayoutEffect(() => {
    clearTimeout(doneTimeout.current)

    const started = nprogress.isStarted()

    if (!started && count.current)
      nprogress.start()

    else if (started && !count.current)
      doneTimeout.current = window.setTimeout(() => nprogress.done(), 200)
  }, [count.current]) // eslint-disable-line

  const rerender = useUpdate()
  const setCount = useCallback((get: (c: number) => number) => { count.current = get(count.current); rerender() }, [rerender])

  return <ProgressContext.Provider children={children} value={useMemo(() => ({
    processes: count.current,
    color, setColor,

    start: () => setCount(c => c + 1),
    stop: () => setCount(c => Math.max(0, c - 1)),
    clear: () => setCount(() => 0)
  }), [
    color,
    setCount
  ])} />
}
