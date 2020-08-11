import React, { useMemo, useState, useRef, ReactNode } from 'react'
import { Book, BookContent } from 'nhitomi-api'
import { SmallBreakpoints, LargeBreakpoints, getBreakpoint, ScreenBreakpoint } from '../../LayoutManager'
import { cx, css } from 'emotion'
import { CoverImage } from '../CoverImage'
import { useClient } from '../../ClientManager'
import { useSpring, animated } from 'react-spring'
import { BookReaderLink } from '../../BookReader'
import VisibilitySensor from 'react-visibility-sensor'

export const Grid = ({ items, contentSelector, width, children }: {
  items: Book[]
  contentSelector: (book: Book) => BookContent
  width: number
  children?: ReactNode
}) => {
  const { spacing, rowWidth, itemWidth, itemHeight } = useMemo(() => {
    let spacing: number
    let rowItems: number
    let rowWidth: number

    if (width < ScreenBreakpoint) {
      const breakpoint = getBreakpoint(SmallBreakpoints, width) || 0

      spacing = 4
      rowItems = SmallBreakpoints.indexOf(breakpoint) + 2
      rowWidth = width
    }
    else {
      const breakpoint = getBreakpoint(LargeBreakpoints, width) || 0

      spacing = 6
      rowItems = LargeBreakpoints.indexOf(breakpoint) + 3
      rowWidth = breakpoint
    }

    const itemWidth = (rowWidth - spacing * (rowItems + 1)) / rowItems
    const itemHeight = itemWidth * 7 / 5

    return { spacing, rowItems, rowWidth, itemWidth, itemHeight }
  }, [width])

  return (
    <div style={{ maxWidth: rowWidth }} className='mx-auto w-full'>
      {children}

      <div className={cx('flex flex-row flex-wrap justify-center', css`
        padding: ${spacing / 2}px;
      `)}>

        {useMemo(() => items.map(item => (
          <Item
            key={item.id}
            book={item}
            content={contentSelector(item)}
            width={itemWidth}
            height={itemHeight}
            className={css`margin: ${spacing / 2}px;`} />
        )), [
          contentSelector,
          itemHeight,
          itemWidth,
          items,
          spacing
        ])}
      </div>
    </div>
  )
}

const Item = ({ book, content, width, height, className }: {
  book: Book
  content: BookContent
  width: number
  height: number
  className?: string
}) => {
  const client = useClient()

  const [hovered, setHovered] = useState(false)
  const [visible, setVisible] = useState(false)
  const visibleEver = useRef(false)

  const overlayStyle = useSpring({
    opacity: hovered ? 1 : 0,
    marginBottom: hovered ? 0 : -5
  })

  return (
    <VisibilitySensor
      delayedCall
      partialVisibility
      offset={{ top: -100, bottom: -100 }}
      onChange={v => { setVisible(v); v && (visibleEver.current = v) }}>

      <div
        style={{ width, height }}
        className={cx('rounded overflow-hidden relative cursor-pointer', className)}
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}>

        <BookReaderLink id={book.id} contentId={content.id}>
          {visibleEver.current && (
            <CoverImage
              className='w-full h-full rounded overflow-hidden'
              onLoad={async () => await client.book.getBookImage({
                id: book.id,
                contentId: content.id,
                index: -1
              })} />
          )}

          {visible && (
            <animated.div style={overlayStyle} className='absolute bottom-0 left-0 w-full'>
              <div className='p-1 bg-white bg-blur text-black rounded-b'>
                <span className='block text-sm truncate font-bold'>{book.primaryName}</span>

                {book.primaryName !== book.englishName && (
                  <span className='block text-xs truncate'>{book.englishName}</span>
                )}
              </div>
            </animated.div>
          )}
        </BookReaderLink>
      </div>
    </VisibilitySensor>
  )
}
