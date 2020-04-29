import React, { useContext, useLayoutEffect, useState, useMemo, useEffect } from 'react'
import { Book, BookContent, Client } from '../Client'
import { useTabTitle } from '../hooks'
import { Prefetch, PrefetchLink, PrefetchLinkProps, usePrefetch } from '../Prefetch'
import { Header } from './Header'
import { LayoutContext } from '../LayoutContext'
import { NotificationContext } from '../NotificationContext'
import { FetchManager, FetchImage } from './fetchManager'
import { ClientContext } from '../ClientContext'
import { LayoutRenderer } from './LayoutRenderer'

type Fetched = {
  book: Book
  content: BookContent
}

export function getBookReaderPrefetch(id: string, contentId: string): Prefetch<Fetched> {
  return {
    path: `/books/${id}/contents/${contentId}`,

    func: async client => {
      const book = await client.book.getBook({ id })
      const content = book.contents.find(c => c.id === contentId)

      if (!content)
        throw Error(`Content ${contentId} does not exist in the book ${id}.`)

      return { book, content }
    }
  }
}

export const BookReader = ({ id, contentId }: { id: string, contentId: string }) => {
  const { result } = usePrefetch(getBookReaderPrefetch(id, contentId))

  if (result)
    return <Loaded {...result} />

  return null
}

export const BookReaderLink = ({ id, contentId, ...props }: PrefetchLinkProps & { id: string, contentId: string }) =>
  <PrefetchLink fetch={getBookReaderPrefetch(id, contentId)} {...props} />

const Loaded = ({ book, content }: Fetched) => {
  useTabTitle(book.primaryName)

  const client = useContext(ClientContext)
  const { mobile } = useContext(LayoutContext)
  const { alert } = useContext(NotificationContext)

  const [cursorHidden, setCursorHidden] = useState(false)
  const [currentPage, setCurrentPage] = useState([content.pages[0]])

  const [fetched, setFetched] = useState<(FetchImage | undefined)[]>([])
  const fetch = useMemo(() => new FetchManager(client, book, content, 5, setFetched), []) // eslint-disable-line

  useEffect(() => {
    fetch.start()

    return () => fetch.destroy()
  }, [fetch])

  // automatically switch to single-page on mobile
  // const initialLayout = useRef(true)
  //
  // useLayoutEffect(() => {
  //   if (mobile) {
  //     setDisplayMode('flow')
  //     setPageMode('single')
  //     setFluid(true)
  //     setSnapping(true)

  //     if (!initialLayout.current)
  //       alert.info('Switched to mobile layout.')
  //   }
  //   else {
  //     setDisplayMode('flow')
  //     setPageMode('double')
  //     setFluid(false)
  //     setSnapping(false)

  //     if (!initialLayout.current)
  //       alert.info('Switched to desktop layout.')
  //   }

  //   initialLayout.current = false // don't alert on initial layout
  // }, [mobile]) // eslint-disable-line

  // hide cursor when current page changes
  useLayoutEffect(() => setCursorHidden(true), [currentPage])

  // key handling
  // useShortcut('bookReaderPageModeKey', () => {
  //   const value = pageMode === 'single' ? 'double' : 'single'

  //   setPageMode(value)
  //   alert.info(`Switched to ${value} page layout.`)
  // })

  // useShortcut('bookReaderFluidKey', () => {
  //   const value = !fluid

  //   setFluid(value)
  //   alert.info(`${value ? 'Enabled' : 'Disabled'} fluid mode.`)
  // })

  // useShortcut('bookReaderLeftToRightKey', () => {
  //   const value = !leftToRight

  //   setLeftToRight(value)
  //   alert.info(`Pages are now ordered ${value ? 'left to right.' : 'right to left.'}`)
  // })

  // useShortcut('bookReaderDoubleCoverKey', () => {
  //   const value = !doubleCover

  //   setDoubleCover(value)
  //   alert.info(`Cover page is now ${value ? 'grouped' : 'independent'}.`)
  // })

  // useShortcut('bookReaderSnappingKey', () => {
  //   const value = !snapping

  //   setSnapping(value)
  //   alert.info(`${value ? 'Enabled' : 'Disabled'} scroll snapping.`)
  // })

  return <>
    <Header book={book} content={content} />

    <div
      onMouseMove={() => { cursorHidden && setCursorHidden(false) }}
      style={{
        cursor: cursorHidden ? 'none' : undefined
      }}>

      <LayoutRenderer book={book} content={content} fetched={fetched} />
    </div>
  </>
}
