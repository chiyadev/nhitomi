import React, { ReactNode, useState, useRef } from 'react'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { StripWidth } from '../Sidebar/Strip'

const breakpoints = [640, 768, 1024, 1280].map(n => n - StripWidth)

export const Container = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const measureRef = useRef<HTMLDivElement>(null)
  const [parentWidth, setParentWidth] = useState(0)

  useResizeObserver(measureRef, ({ contentRect: { width } }) => setParentWidth(width))

  let width: number | undefined

  for (const breakpoint of breakpoints) {
    if (parentWidth >= breakpoint)
      width = breakpoint

    else break
  }

  return (
    <div ref={measureRef} className='w-full'>
      <div style={{ maxWidth: width }} className={cx('relative mx-auto w-full', className)} children={children} />
    </div>
  )
}
