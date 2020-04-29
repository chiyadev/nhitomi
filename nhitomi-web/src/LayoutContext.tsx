import React, { createContext, ReactNode, useMemo, useState } from 'react'
import { useWindowSize } from 'react-use'
import { SideBarWidth } from './Sidebar'

/** Layout information context. */
export const LayoutContext = createContext<{
  width: number
  height: number
  mobile: boolean
  breakpoint: boolean
  sidebar: boolean
  setSidebar: (v: boolean) => void
}>(undefined as any)

const mdBreakpoint = 768

export const LayoutProvider = ({ children }: { children?: ReactNode }) => {
  const { width: windowWidth, height: windowHeight } = useWindowSize()

  const breakpoint = windowWidth < mdBreakpoint
  const [sidebar, setSidebar] = useState(!breakpoint)

  // mobile is determined by window orientation rather than resolution
  // this allows the reader UI to be displayed as desktop mode in landscape
  const mobile = windowHeight > windowWidth

  const width = sidebar && !breakpoint ? windowWidth - SideBarWidth : windowWidth
  const height = windowHeight

  return <LayoutContext.Provider children={children} value={useMemo(() => ({
    width,
    height,
    mobile,
    breakpoint,
    sidebar,
    setSidebar
  }), [
    width,
    height,
    mobile,
    breakpoint,
    sidebar,
    setSidebar
  ])} />
}
