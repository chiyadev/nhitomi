import React, { createContext, ReactNode, useRef, useLayoutEffect, useMemo, useContext } from 'react'
import nprogress from 'nprogress'
import { useConfig } from './ConfigManager'

import './Progress.css'

const ProgressContext = createContext<{
  begin: () => void
  end: () => void
}>(undefined as any)

export function useProgress() {
  return useContext(ProgressContext)
}

export const ProgressManager = ({ children }: { children?: ReactNode }) => {
  const count = useRef(0)
  const [mode] = useConfig('animation')

  useLayoutEffect(() => {
    let easing: string

    switch (mode) {
      case 'normal': easing = 'ease'; break
      case 'faster': easing = 'cubic-bezier(0, 1, 0, 1)'; break
      case 'none': easing = 'steps-start'; break
    }

    nprogress.configure({
      template: `
        <div class="bar" role="bar">
          <div class="peg"></div>
        </div>
        <div class="spinner" role="spinner">
          <div class="spinner-icon"></div>
        </div>
      `,
      easing
    })
  }, [mode])

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
