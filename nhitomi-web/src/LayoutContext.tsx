import React, { createContext, ReactNode, useMemo, useRef } from 'react'
import { useWindowSize } from 'react-use'
import { SideBarWidth } from './Sidebar'
import { Breakpoint } from './LayoutContext'
import { useConfig } from './Client/config'

export type { Breakpoint } from 'antd/lib/_util/responsiveObserve'

/** Layout information context. */
export const LayoutContext = createContext<{
  width: number
  height: number
  mobile: boolean
  breakpoint: boolean
  sidebar: boolean
  setSidebar: (v: boolean) => void

  getBreakpoint: (width: number) => Breakpoint
}>(undefined as any)

const breakpoints: { [key in Breakpoint]: number } = {
  xs: 480,
  sm: 576,
  md: 768,
  lg: 992,
  xl: 1200,
  xxl: 1600
}

function getBreakpoint(width: number) {
  let breakpoint: Breakpoint = 'xxl'

  for (const key in breakpoints) {
    if (width < breakpoints[key as Breakpoint]) {
      breakpoint = key as Breakpoint
      break
    }
  }

  return breakpoint
}

export const LayoutProvider = ({ children }: { children?: ReactNode }) => {
  let { width: windowWidth, height: windowHeight } = useWindowSize()

  // use an element to measure window size if possible
  // this excludes scrollbar from width and allows zooming to work on mobile without breaking
  const ref = useRef<HTMLDivElement>(null)

  if (ref.current) {
    windowWidth = ref.current.clientWidth
    windowHeight = ref.current.clientHeight
  }

  const breakpoint = Object.keys(breakpoints).indexOf(getBreakpoint(windowWidth)) < 3
  const [sidebar, setSidebar] = useConfig('sidebar')

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
      setSidebar,
      getBreakpoint
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
