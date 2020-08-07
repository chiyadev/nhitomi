import React, { ReactNode, createContext, useMemo, useContext } from 'react'
import { useWindowSize } from 'react-use'
import { StripWidth } from './Sidebar/Strip'

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

export const Breakpoint = 768

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
