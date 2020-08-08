import React, { ReactNode, createContext, useMemo, useContext } from 'react'
import { useWindowSize } from 'react-use'
import { StripWidth } from './Sidebar/Strip'

/** Represents the width after which the device will be considered large. */
export const Breakpoint = 768

export const SmallBreakpoints = [320, 480, 640]
export const LargeBreakpoints = [640, 768, 1024, 1280].map(n => n - StripWidth)

export function getBreakpoint(breakpoints: number[], value: number) {
  let breakpoint: number | undefined

  for (const br of breakpoints) {
    if (value >= br)
      breakpoint = br

    else break
  }

  return breakpoint
}

/** Layout information context. */
const LayoutContext = createContext<{
  width: number
  height: number
  sidebar: number
  screen: 'sm' | 'lg'
}>(undefined as any)

export function useLayout() {
  return useContext(LayoutContext)
}

export const LayoutManager = ({ children }: { children?: ReactNode }) => {
  const { width, height } = useWindowSize()

  return (
    <LayoutContext.Provider
      value={useMemo(() => ({
        width: width >= Breakpoint ? width - StripWidth : width,
        height,
        sidebar: width >= Breakpoint ? StripWidth : 0,
        screen: width >= Breakpoint ? 'lg' : 'sm'
      }), [
        width,
        height
      ])}
      children={children} />
  )
}
