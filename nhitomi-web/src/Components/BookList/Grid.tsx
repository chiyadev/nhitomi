import React, { useMemo, useState, ReactNode } from 'react'
import { SmallBreakpoints, LargeBreakpoints, getBreakpoint, ScreenBreakpoint, useLayout } from '../../LayoutManager'
import { cx, css } from 'emotion'
import { CoverImage } from '../CoverImage'
import { useClient } from '../../ClientManager'
import { useSpring, animated } from 'react-spring'
import { BookReaderLink } from '../../BookReader'
import VisibilitySensor from 'react-visibility-sensor'
import { useBookList, BookListItem, useContentSelector } from '.'
import { BookContent } from 'nhitomi-api'
import { useConfig } from '../../ConfigManager'
import { ContextMenu } from '../ContextMenu'
import { Overlay } from './Overlay'

export const Grid = ({ width, menu, empty }: {
  width: number
  menu?: ReactNode
  empty?: ReactNode
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
      <Menu children={menu} />

      {useMemo(() => (
        <div className={cx('flex flex-row flex-wrap justify-center', css`
          padding-left: ${spacing / 2}px;
          padding-right: ${spacing / 2}px;
        `)}>

          {items.map(item => (
            <Item
              key={item.id}
              book={item}
              width={itemWidth}
              height={itemHeight}
              className={css`margin: ${spacing / 2}px;`} />
          ))}

          {!items.length && empty}
        </div>
      ), [empty, itemHeight, itemWidth, items, spacing])}
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
        className='w-full flex flex-row justify-end px-2 mb-2'
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
  const { screen, height: screenHeight } = useLayout()
  const contentSelector = useContentSelector()
  const { LinkComponent } = useBookList()
  const [hover, setHover] = useState(false)
  const [showImage, setShowImage] = useState(false)

  const content = useMemo(() => contentSelector(book.contents), [book.contents, contentSelector])

  const overlay = useMemo(() => (
    <ItemOverlay book={book} hover={hover} />
  ), [book, hover])

  const image = useMemo(() => showImage && (
    <ItemCover book={book} content={content} />
  ), [book, content, showImage])

  const inner = useMemo(() => {
    const children = <>{image}{overlay}</>

    return (
      <div
        style={{ width, height }}
        className={cx('rounded overflow-hidden relative', className)}
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}>

        {LinkComponent
          ? <LinkComponent id={book.id} contentId={content?.id} children={children} />
          : content
            ? <BookReaderLink id={book.id} contentId={content.id} children={children} />
            : children}
      </div>
    )
  }, [LinkComponent, book.id, className, content, height, image, overlay, width])

  let preload: number

  switch (screen) {
    case 'sm': preload = screenHeight * 2; break
    case 'lg': preload = 400; break
  }

  return useMemo(() => (
    <ContextMenu overlay={(
      <Overlay book={book} content={content} />
    )}>
      <VisibilitySensor
        delayedCall
        partialVisibility
        offset={{ top: -preload, bottom: -preload }}
        onChange={v => { v && setShowImage(true) }}
        children={inner} />
    </ContextMenu>
  ), [book, content, inner, preload])
}

const ItemCover = ({ book, content }: { book: BookListItem, content?: BookContent }) => {
  const client = useClient()
  const { getCoverRequest } = useBookList()

  return useMemo(() => content
    ? (
      <CoverImage
        key={`${book.id}/${content.id}`}
        cacheKey={`books/${book.id}/contents/${content.id}/pages/-1`}
        zoomIn
        className='w-full h-full rounded overflow-hidden'
        onLoad={async () => await client.book.getBookImage(getCoverRequest?.(book, content) || { id: book.id, contentId: content.id, index: -1 })} />
    ) : (
      <div className='w-full h-full' />
    ), [book, client.book, content, getCoverRequest])
}

const ItemOverlay = ({ book, hover }: { book: BookListItem, hover?: boolean }) => {
  let [preferEnglishName] = useConfig('bookReaderPreferEnglishName')

  const { overlayVisible, preferEnglishName: preferEnglishNameOverride } = useBookList()
  hover = overlayVisible || hover

  if (typeof preferEnglishNameOverride !== 'undefined')
    preferEnglishName = preferEnglishNameOverride

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
    <div className='px-2 py-1 bg-white bg-blur text-black rounded-b'>
      <span className='block truncate font-bold'>{(preferEnglishName && book.englishName) || book.primaryName}</span>

      {book.englishName && book.primaryName !== book.englishName && (
        <span className='block text-sm truncate'>{(!preferEnglishName && book.englishName) || book.primaryName}</span>
      )}
    </div>
  ), [book.englishName, book.primaryName, preferEnglishName, visible])

  if (!visible)
    return null

  return (
    <animated.div style={style} className='absolute bottom-0 left-0 w-full' children={inner} />
  )
}
