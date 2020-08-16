import React, { useMemo, useState, ReactNode } from 'react'
import { SmallBreakpoints, LargeBreakpoints, getBreakpoint, ScreenBreakpoint } from '../../LayoutManager'
import { cx, css } from 'emotion'
import { CoverImage } from '../CoverImage'
import { useClient } from '../../ClientManager'
import { useSpring, animated } from 'react-spring'
import { BookReaderLink } from '../../BookReader'
import VisibilitySensor from 'react-visibility-sensor'
import { useBookList, BookListItem } from '.'
import { BookContent } from 'nhitomi-api'

export const Grid = ({ width, children }: {
  width: number
  children?: ReactNode
}) => {
  const { items } = useBookList()

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
      <Menu children={children} />

      <div className={cx('flex flex-row flex-wrap justify-center', css`
        padding-left: ${spacing / 2}px;
        padding-right: ${spacing / 2}px;
      `)}>

        {useMemo(() => items.map(item => (
          <Item
            key={item.id}
            book={item}
            width={itemWidth}
            height={itemHeight}
            className={css`margin: ${spacing / 2}px;`} />
        )), [itemHeight, itemWidth, items, spacing])}
      </div>
    </div>
  )
}

const Menu = ({ children }: { children?: ReactNode }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 }
  })

  return <>
    {children && (
      <animated.div
        style={style}
        className='w-full flex flex-row justify-end px-1 mb-1'
        children={children} />
    )}
  </>
}

const Item = ({ book, width, height, className }: {
  book: BookListItem
  width: number
  height: number
  className?: string
}) => {
  const { contentSelector, LinkComponent } = useBookList()
  const [hover, setHover] = useState(false)
  const [showImage, setShowImage] = useState(false)

  const content = useMemo(() => contentSelector(book), [book, contentSelector])

  const overlay = useMemo(() => <ItemOverlay book={book} hover={hover} />, [book, hover])
  const image = useMemo(() => showImage && <ItemCover book={book} content={content} />, [book, content, showImage])
  const inner = useMemo(() => <>{image}{overlay}</>, [image, overlay])

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

        {LinkComponent
          ? <LinkComponent id={book.id} contentId={content?.id} children={inner} />
          : content
            ? <BookReaderLink id={book.id} contentId={content.id} children={inner} />
            : inner}
      </div>
    </VisibilitySensor>
  ), [LinkComponent, book.id, className, content, height, inner, width])
}

const ItemCover = ({ book, content }: { book: BookListItem, content?: BookContent }) => {
  const client = useClient()
  const { getCoverRequest } = useBookList()

  return useMemo(() => content
    ? (
      <CoverImage
        key={`${book.id}/${content.id}`}
        zoomIn
        className='w-full h-full rounded overflow-hidden'
        onLoad={async () => await client.book.getBookImage(getCoverRequest?.(book, content) || { id: book.id, contentId: content.id, index: -1 })} />
    ) : (
      <div className='w-full h-full' />
    ), [book, client.book, content, getCoverRequest])
}

const ItemOverlay = ({ book, hover }: { book: BookListItem, hover?: boolean }) => {
  const { overlayVisible } = useBookList()
  hover = overlayVisible || hover

  const [visible, setVisible] = useState(hover)
  const style = useSpring({
    from: {
      opacity: 0
    },
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
