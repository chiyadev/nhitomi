import { RefObject, useContext, useRef } from 'react'
import { LayoutResult } from './layoutManager'
import { useWindowScroll } from 'react-use'
import { LayoutContext } from '../LayoutContext'
import { BookReaderContext } from '.'

export const ScrollManager = ({ containerRef, layout }: { containerRef: RefObject<HTMLDivElement>, layout: LayoutResult }) => {
  const { height } = useContext(LayoutContext)
  const { currentRow, setCurrentRow } = useContext(BookReaderContext)

  // scroll position relative to layout container
  const layoutOffset = containerRef.current?.offsetTop || 0

  // detect passive current row
  const mid = useWindowScroll().y - layoutOffset + height / 2

  let current = 0

  findRow:
  for (; current < layout.rows.length; current++) {
    const row = layout.rows[current]

    // consider first row in the middle of viewport to be the current row
    for (const image of row.images) {
      if (image.y <= mid && mid < image.y + image.height) {
        if (currentRow.passive !== current)
          setCurrentRow({ ...currentRow, passive: current })

        break findRow
      }
    }
  }

  // scroll to induced current row
  const lastInduced = useRef(currentRow.induced)

  if (lastInduced.current !== currentRow.induced) {
    lastInduced.current = currentRow.induced

    const row = layout.rows[Math.max(0, Math.min(layout.rows.length - 1, currentRow.induced))]
    let rowMid = 0

    for (const image of row.images)
      rowMid += image.y + image.height / 2

    rowMid /= row.images.length

    window.scrollTo({ top: layoutOffset + rowMid - height / 2 })
  }

  return null
}
