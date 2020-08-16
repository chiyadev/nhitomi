import { BookContent, LanguageType } from 'nhitomi-api'
import React, { useRef, useState, ReactNode, createContext, useMemo, ContextType, useContext, ComponentType } from 'react'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { Grid } from './Grid'
import { ScraperTypes } from '../../orderedConstants'

export type BookListItem = {
  id: string
  primaryName: string
  englishName?: string
  contents: BookContent[]
}

const BookListContext = createContext<{
  items: BookListItem[]
  contentSelector: (book: BookListItem) => BookContent | undefined
  getItemId?: (book: BookListItem, content?: BookContent) => { id: string, contentId?: string }
  overlayVisible?: boolean

  LinkComponent?: ComponentType<{ id: string, contentId?: string }>
}>(undefined as any)

export function useBookList() {
  return useContext(BookListContext)
}

export const BookList = ({ items, contentSelector, getItemId, overlayVisible, LinkComponent, className, children }: ContextType<typeof BookListContext> & {
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

      <BookListContext.Provider value={useMemo(() => ({
        items,
        contentSelector,
        getItemId,
        overlayVisible,
        LinkComponent
      }), [LinkComponent, contentSelector, getItemId, items, overlayVisible])}>

        {width && (
          <Grid width={width} children={children} />
        )}
      </BookListContext.Provider>
    </div>
  )
}

export function selectContent(contents: BookContent[], languages: LanguageType[] = []): BookContent | undefined {
  return contents.sort((a, b) => {
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
