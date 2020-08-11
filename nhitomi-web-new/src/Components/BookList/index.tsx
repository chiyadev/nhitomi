import { Book, BookContent } from 'nhitomi-api'
import React, { useRef, useState, ReactNode } from 'react'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { Grid } from './Grid'

export const BookList = ({ items, contentSelector, className, children }: {
  items: Book[]
  contentSelector: (book: Book) => BookContent
  className?: string
  children?: ReactNode
}) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(containerRef.current?.clientWidth)

  useResizeObserver(containerRef, ({ contentRect: { width } }) => setWidth(width))

  return (
    <div
      ref={containerRef}
      className={cx('w-full relative', className)}>

      {width && (
        <Grid
          items={items}
          contentSelector={contentSelector}
          width={width}
          children={children} />
      )}
    </div>
  )
}
