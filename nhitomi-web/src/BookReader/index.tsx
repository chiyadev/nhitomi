import React, { useContext, useLayoutEffect, useState, useMemo, createContext, Dispatch, useCallback } from 'react'
import { Book, BookContent } from '../Client'
import { useTabTitle } from '../hooks'
import { Prefetch, PrefetchLink, PrefetchLinkProps, usePrefetch } from '../Prefetch'
import { Header } from './Header'
import { LayoutContext } from '../LayoutContext'
import { NotificationContext } from '../NotificationContext'
import { FetchManager, FetchImage } from './fetchManager'
import { ClientContext } from '../ClientContext'
import { LayoutRenderer } from './LayoutRenderer'
import { useConfig } from '../Client/config'
import { useShortcut } from '../shortcuts'
import { FormattedMessage } from 'react-intl'
import { JumpModal } from './JumpModal'
import { Details } from './Details'

type Fetched = {
  book: Book
  content: BookContent
}

export function getBookReaderPrefetch(id: string, contentId: string): Prefetch<Fetched> {
  return {
    path: `/books/${id}/contents/${contentId}`,
    getPath: ({ book, content }) => `/books/${book.id}/contents/${content.id}`,

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
  const { result, dispatch } = usePrefetch(getBookReaderPrefetch(id, contentId))

  if (result)
    return <Loaded {...result} dispatch={dispatch} />

  return null
}

export const BookReaderLink = ({ id, contentId, ...props }: PrefetchLinkProps & { id: string, contentId: string }) =>
  <PrefetchLink fetch={getBookReaderPrefetch(id, contentId)} {...props} />

type CurrentPage = {
  rowInduced: number
  rowPassive: number

  pageInduced: number
  pagePassive: number
}

export const BookReaderContext = createContext<{
  book: Book
  content: BookContent
  setContent: (content: BookContent) => void

  fetch: FetchManager

  currentPage: CurrentPage
  setCurrentPage: (page: CurrentPage) => void

  jump: boolean
  setJump: (jump: boolean) => void
}>(undefined as any)

const Loaded = ({ book, content, dispatch }: Fetched & { dispatch: Dispatch<Fetched> }) => {
  useTabTitle(book.primaryName)

  const client = useContext(ClientContext)
  const { mobile, breakpoint } = useContext(LayoutContext)
  const { alert } = useContext(NotificationContext)

  const [cursorHidden, setCursorHidden] = useState(false)
  const [currentPage, setCurrentPage] = useState<CurrentPage>({ rowInduced: 0, rowPassive: 0, pageInduced: 0, pagePassive: 0 })
  const [jump, setJump] = useState(false)

  // hide cursor when current row changes
  useLayoutEffect(() => setCursorHidden(true), [currentPage])

  const [fetched, setFetched] = useState<(FetchImage | undefined)[]>([])
  const fetch = useMemo(() => new FetchManager(client, book, content, 5, setFetched), [book, client, content])

  useLayoutEffect(() => {
    fetch.start()
    return () => fetch.destroy()
  }, [fetch])

  const [imagesPerRow, setImagesPerRow] = useConfig('bookReaderImagesPerRow')
  const [viewportBound, setViewportBound] = useConfig('bookReaderViewportBound')
  const [leftToRight, setLeftToRight] = useConfig('bookReaderLeftToRight')
  const [singleCover, setSingleCover] = useConfig('bookReaderSingleCover')

  // automatically adjust layout for mobile
  useLayoutEffect(() => {
    if (mobile) {
      setImagesPerRow(1)
      setViewportBound(false)
    }
    else {
      setImagesPerRow(2)
      setViewportBound(!breakpoint)
    }
  }, [mobile, breakpoint]) // eslint-disable-line

  // key handling
  useShortcut('bookReaderFirstPageKey', () => setCurrentPage({ ...currentPage, rowInduced: 0 }))
  useShortcut('bookReaderLastPageKey', () => setCurrentPage({ ...currentPage, rowInduced: fetched.length - 1 }))
  useShortcut('bookReaderPreviousPageKey', () => setCurrentPage({ ...currentPage, rowInduced: currentPage.rowPassive - 1 }))
  useShortcut('bookReaderNextPageKey', () => setCurrentPage({ ...currentPage, rowInduced: currentPage.rowPassive + 1 }))

  useShortcut('bookReaderImagesPerRowKey', () => {
    const value = imagesPerRow === 1 ? 2 : 1
    setImagesPerRow(value)
    alert.info(<FormattedMessage id='bookReader.alerts.imagesPerRow' values={{ value }} />)
  })

  useShortcut('bookReaderViewportBoundKey', () => {
    const value = !viewportBound
    setViewportBound(value)
    alert.info(<FormattedMessage id={`bookReader.alerts.viewport${value ? 'Bound' : 'Unbound'}`} />)
  })

  useShortcut('bookReaderLeftToRightKey', () => {
    const value = !leftToRight
    setLeftToRight(value)
    alert.info(<FormattedMessage id={`bookReader.alerts.${value ? 'leftToRight' : 'rightToLeft'}`} />)
  })

  useShortcut('bookReaderSingleCoverKey', () => {
    const value = !singleCover
    setSingleCover(value)
    alert.info(<FormattedMessage id={`bookReader.alerts.singleCover${value ? 'Enabled' : 'Disabled'}`} />)
  })

  const [details, setDetails] = useState(false)
  const setContent = useCallback((content: BookContent) => dispatch({ book, content }), [book, dispatch])

  return (
    <BookReaderContext.Provider value={useMemo(() => ({
      book,
      content, setContent,
      fetch,
      currentPage, setCurrentPage,
      jump, setJump
    }), [
      book,
      content, setContent,
      fetch,
      currentPage, setCurrentPage,
      jump, setJump
    ])}>
      <Details open={details} setOpen={setDetails} book={book} content={content} setContent={setContent} />
      <Header setDetails={setDetails} />
      <JumpModal />

      <div
        onMouseMove={() => { cursorHidden && setCursorHidden(false) }}
        style={{ cursor: cursorHidden ? 'none' : undefined }}>

        <LayoutRenderer fetched={fetched} />
      </div>
    </BookReaderContext.Provider>
  )
}
