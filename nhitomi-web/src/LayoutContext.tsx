import React, { createContext, ReactNode, useMemo, useState, useRef } from 'react'
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
  let { width: windowWidth, height: windowHeight } = useWindowSize()

  // use an element to measure window size if possible
  // this excludes scrollbar from width and allows zooming to work on mobile without breaking
  const ref = useRef<HTMLDivElement>(null)

  if (ref.current) {
    windowWidth = ref.current.clientWidth
    windowHeight = ref.current.clientHeight
  }

  const breakpoint = windowWidth < mdBreakpoint
  const [sidebar, setSidebar] = useState(!breakpoint)

  // mobile is determined by window orientation rather than resolution
  // this allows the reader UI to be displayed as desktop mode in landscape
  const mobile = windowHeight > windowWidth

  const width = sidebar && !breakpoint ? windowWidth - SideBarWidth : windowWidth
  const height = windowHeight

  return <>
    <div ref={ref} id='measurer' style={{
      pointerEvents: 'none',
      position: 'absolute',
      left: 0,
      top: 0,
      width: '100%',
      height: '100%'
    }} />

    <LayoutContext.Provider children={children} value={useMemo(() => ({
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
  </>
}
