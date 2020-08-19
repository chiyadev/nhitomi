import React, { useRef, useState, ReactNode, createContext, useMemo, ContextType, useContext, ComponentType } from 'react'
import { BookContent, LanguageType, BookApiGetBookImageRequest, BookTags } from 'nhitomi-api'
import { cx } from 'emotion'
import useResizeObserver from '@react-hook/resize-observer'
import { Grid } from './Grid'
import { ScraperTypes } from '../../orderedConstants'
import { TypedPrefetchLinkProps } from '../../Prefetch'

export type BookListItem = {
  id: string
  primaryName: string
  englishName?: string
  contents: BookContent[]
  tags?: BookTags
}

const BookListContext = createContext<{
  items: BookListItem[]
  contentSelector: (book: BookListItem) => BookContent | undefined
  getCoverRequest?: (book: BookListItem, content: BookContent) => BookApiGetBookImageRequest
  preferEnglishName?: boolean
  overlayVisible?: boolean

  LinkComponent?: ComponentType<{ id: string, contentId?: string } & TypedPrefetchLinkProps>
}>(undefined as any)

export function useBookList() {
  return useContext(BookListContext)
}

export const BookList = ({ className, menu, empty, ...context }: ContextType<typeof BookListContext> & {
  className?: string
  menu?: ReactNode
  empty?: ReactNode
}) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(containerRef.current?.clientWidth)

  useResizeObserver(containerRef, ({ contentRect: { width } }) => setWidth(width))

  const { items, contentSelector, getCoverRequest, preferEnglishName, overlayVisible, LinkComponent } = context
  context = useMemo(() => ({ items, contentSelector, getCoverRequest, preferEnglishName, overlayVisible, LinkComponent }), [LinkComponent, contentSelector, getCoverRequest, items, overlayVisible, preferEnglishName])

  return (
    <div
      ref={containerRef}
      className={cx('w-full relative', className)}>

      <BookListContext.Provider value={context}>
        {width && (
          <Grid width={width} menu={menu} empty={empty} />
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
