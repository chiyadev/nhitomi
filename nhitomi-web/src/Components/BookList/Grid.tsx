import React, { useMemo, useState, ReactNode } from 'react'
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
        )), [contentSelector, itemHeight, itemWidth, items, spacing])}
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
  const [hover, setHover] = useState(false)
  const [showImage, setShowImage] = useState(false)

  const overlay = useMemo(() => <ItemOverlay book={book} content={content} hover={hover} />, [book, content, hover])
  const image = useMemo(() => showImage && <ItemCover book={book} content={content} />, [book, content, showImage])

  return useMemo(() => (
    <VisibilitySensor
      delayedCall
      partialVisibility
      offset={{ top: -200, bottom: -200 }}
      onChange={v => { v && setShowImage(true) }}>

      <div
        style={{ width, height }}
        className={cx('rounded overflow-hidden relative cursor-pointer', className)}
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}>

        <BookReaderLink id={book.id} contentId={content.id}>
          {image}
          {overlay}
        </BookReaderLink>
      </div>
    </VisibilitySensor>
  ), [book.id, className, content.id, height, image, overlay, width])
}

const ItemCover = ({ book, content }: { book: Book, content: BookContent }) => {
  const client = useClient()

  return useMemo(() => (
    <CoverImage
      key={`${book.id}/${content.id}`}
      zoomIn
      className={cx('w-full h-full rounded overflow-hidden')}
      onLoad={async () => await client.book.getBookImage({
        id: book.id,
        contentId: content.id,
        index: -1
      })} />
  ), [book.id, client.book, content.id])
}

const ItemOverlay = ({ book, content, hover }: { book: Book, content: BookContent, hover?: boolean }) => {
  const [visible, setVisible] = useState(false)
  const style = useSpring({
    opacity: hover ? 1 : 0,
    marginBottom: hover ? 0 : -5,
    onChange: {
      opacity: v => setVisible(v > 0)
    }
  })

  const inner = useMemo(() => visible && (
    <div className='p-1 bg-white bg-blur text-black rounded-b'>
      <span className='block text-sm truncate font-bold'>{book.primaryName}</span>

      {book.primaryName !== book.englishName && (
        <span className='block text-xs truncate'>{book.englishName}</span>
      )}
    </div>
  ), [book.englishName, book.primaryName, visible])

  if (!visible)
    return null

  return (
    <animated.div style={style} className='absolute bottom-0 left-0 w-full' children={inner} />
  )
}
