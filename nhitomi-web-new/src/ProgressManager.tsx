import React, { createContext, ReactNode, useRef, useLayoutEffect, useMemo } from 'react'
import nprogress from 'nprogress'

import './Progress.css'

export const ProgressContext = createContext<{
  begin: () => void
  end: () => void
}>(undefined as any)

export const ProgressManager = ({ children }: { children?: ReactNode }) => {
  const count = useRef(0)

  useLayoutEffect(() => {
    nprogress.configure({
      template: `
        <div class="bar" role="bar">
          <div class="peg"></div>
        </div>
        <div class="spinner" role="spinner">
          <div class="spinner-icon"></div>
        </div>
      `
    })
  }, [])

  // timeout prevents flickering when fetching multiple resources in a short time
  const done = useRef<number>()

  return (
    <ProgressContext.Provider
      value={useMemo(() => ({
        begin: () => {
          clearTimeout(done.current)

          if (count.current++ === 0)
            nprogress.start()
        },
        end: () => {
          if (--count.current === 0)
            done.current = window.setTimeout(() => nprogress.done(), 200)
        }
      }), [])}
      children={children} />
  )
}
