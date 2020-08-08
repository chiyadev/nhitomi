import { Book } from 'nhitomi-api'
import React, { useRef, useState } from 'react'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { Grid } from './Grid'

export const BookList = ({ items, className }: { items: Book[], className?: string }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(containerRef.current?.clientWidth)

  useResizeObserver(containerRef, ({ contentRect: { width } }) => setWidth(width))

  return (
    <div
      ref={containerRef}
      className={cx('w-full relative', className)}>

      {width && (
        <Grid items={items} width={width} />
      )}
    </div>
  )
}
