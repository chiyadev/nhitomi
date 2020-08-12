import { Book, BookContent, LanguageType } from 'nhitomi-api'
import React, { useRef, useState, ReactNode } from 'react'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { Grid } from './Grid'
import { ScraperTypes } from '../../orderedConstants'

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

export function selectContent(book: Book, languages: LanguageType[] = []) {
  return book.contents.sort((a, b) => {
    // respect language preference
    const language = indexCompare(languages, a.language, b.language)
    if (language) return language

    // respect display scraper order
    const source = indexCompare(ScraperTypes, a.source, b.source)
    if (source) return source

    // prefer newer contents
    return b.id.localeCompare(a.id)
  })[0]
}

function indexCompare<T>(array: T[], a: T, b: T) {
  const x = array.indexOf(a)
  const y = array.indexOf(b)

  // prefer existing first
  if (x === -1) return 1
  if (y === -1) return -1

  return x - y
}
