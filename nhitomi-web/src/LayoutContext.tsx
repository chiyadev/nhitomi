import React, { createContext, ReactNode, useMemo, useState } from 'react'
import { Breakpoint } from 'antd/lib/_util/responsiveObserve'
import { useWindowSize } from 'react-use'
import { SideBarWidth } from './Sidebar'

/** Layout information context. */
export const LayoutContext = createContext<{
  width: number
  height: number
  mobile: boolean
  breakpoint: Breakpoint
  sidebar: boolean
  setSidebar: (v: boolean) => void
}>(undefined as any)

export const LayoutProvider = ({ children }: { children?: ReactNode }) => {
  const { width: windowWidth, height: windowHeight } = useWindowSize()

  let breakpoint: Breakpoint

  if (windowWidth < 480) breakpoint = 'xs'
  else if (windowWidth < 576) breakpoint = 'sm'
  else if (windowWidth < 768) breakpoint = 'md'
  else if (windowWidth < 992) breakpoint = 'lg'
  else if (windowWidth < 1200) breakpoint = 'xl'
  else breakpoint = 'xxl'

  const [sidebar, setSidebar] = useState(windowWidth >= 768) // md

  const width = sidebar ? windowWidth - SideBarWidth : windowWidth
  const height = windowHeight

  // mobile is determined by window orientation rather than resolution
  // this allows the reader UI to be displayed as desktop mode in landscape
  const mobile = windowHeight > windowWidth

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
