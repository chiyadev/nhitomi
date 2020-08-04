import React, { ReactNode, createContext, useMemo } from 'react'
import { useWindowSize } from 'react-use'
import { StripWidth } from './Sidebar/Strip'

/** Layout information context. */
export const LayoutContext = createContext<{
  width: number
  height: number
  screen: 'sm' | 'lg'
}>(undefined as any)

export const Breakpoint = 768

export const LayoutManager = ({ children }: { children?: ReactNode }) => {
  const { width, height } = useWindowSize()

  return (
    <LayoutContext.Provider
      value={useMemo(() => ({
        width: width >= Breakpoint ? width - StripWidth : width,
        height,
        screen: width >= Breakpoint ? 'lg' : 'sm'
      }), [
        width,
        height
      ])}
      children={children} />
  )
}
