import { RefObject, useContext, useRef } from 'react'
import { LayoutResult } from './layoutManager'
import { useWindowScroll } from 'react-use'
import { LayoutContext } from '../LayoutContext'
import { BookReaderContext } from '.'

export const ScrollManager = ({ containerRef, layout }: { containerRef: RefObject<HTMLDivElement>, layout: LayoutResult }) => {
  const { height } = useContext(LayoutContext)
  const { currentPage, setCurrentPage } = useContext(BookReaderContext)

  const lastPage = useRef(currentPage)

  // scroll position relative to layout container
  const layoutOffset = containerRef.current?.offsetTop || 0

  // detect passive current row
  const mid = useWindowScroll().y - layoutOffset + height / 2

  let rows = 0
  let pages = 0

  findRow:

  for (const row of layout.rows) {
    for (const image of row.images) {
      // consider first row in the middle of viewport to be the current row
      if (image.y <= mid && mid < image.y + image.height) {
        if (currentPage.rowPassive !== rows || currentPage.pagePassive !== pages)
          setCurrentPage({ ...currentPage, rowPassive: rows, pagePassive: pages })

        break findRow
      }

      pages++
    }

    rows++
  }

  if (lastPage.current.pageInduced !== currentPage.pageInduced) {
    const page = layout.images[Math.max(0, Math.min(layout.images.length - 1, currentPage.pageInduced))]
    const pageMid = page.y + page.height / 2

    window.scrollTo({ top: layoutOffset + pageMid - height / 2 })
  }

  if (lastPage.current.rowInduced !== currentPage.rowInduced) {
    const row = layout.rows[Math.max(0, Math.min(layout.rows.length - 1, currentPage.rowInduced))]
    let rowMid = 0

    for (const image of row.images)
      rowMid += image.y + image.height / 2

    rowMid /= row.images.length

    window.scrollTo({ top: layoutOffset + rowMid - height / 2 })
  }

  lastPage.current = currentPage
  return null
}
