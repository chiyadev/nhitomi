import React, { useMemo, useState } from 'react'
import { Book } from 'nhitomi-api'
import { useLayout, SmallBreakpoints, LargeBreakpoints, getBreakpoint } from '../../LayoutManager'
import { cx, css } from 'emotion'
import { CoverImage } from '../CoverImage'
import { useClient } from '../../ClientManager'
import { useSpring, animated } from 'react-spring'

export const Grid = ({ items, width }: { items: Book[], width: number }) => {
  const { screen } = useLayout()

  const { spacing, rowWidth, itemWidth, itemHeight } = useMemo(() => {
    let spacing: number
    let rowItems: number
    let rowWidth: number

    switch (screen) {
      case 'sm': {
        const breakpoint = getBreakpoint(SmallBreakpoints, width) || 0

        spacing = 4
        rowItems = SmallBreakpoints.indexOf(breakpoint) + 2
        rowWidth = width
        break
      }

      case 'lg': {
        const breakpoint = getBreakpoint(LargeBreakpoints, width) || 0

        spacing = 6
        rowItems = LargeBreakpoints.indexOf(breakpoint) + 3
        rowWidth = breakpoint
        break
      }
    }

    const itemWidth = (rowWidth - spacing * (rowItems + 1)) / rowItems
    const itemHeight = itemWidth * 7 / 5

    return { spacing, rowItems, rowWidth, itemWidth, itemHeight }
  }, [screen, width])

  return (
    <div
      style={{ maxWidth: rowWidth }}
      className={cx('mx-auto w-full flex flex-row flex-wrap justify-center', css`
        padding: ${spacing / 2}px;
      `)}>

      {items.map(item => (
        <Item
          key={item.id}
          book={item}
          width={itemWidth}
          height={itemHeight}
          className={css`margin: ${spacing / 2}px;`} />
      ))}
    </div>
  )
}

const Item = ({ book, width, height, className }: { book: Book, width: number, height: number, className?: string }) => {
  const client = useClient()
  const [hovered, setHovered] = useState(false)
  const overlayStyle = useSpring({
    opacity: hovered ? 1 : 0,
    marginBottom: hovered ? 0 : -5
  })

  return (
    <div
      style={{ width, height }}
      className={cx('rounded overflow-hidden relative cursor-pointer', className)}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <CoverImage
        className='w-full h-full rounded overflow-hidden'
        onLoad={async () => await client.book.getBookImage({
          id: book.id,
          contentId: book.contents[0].id,
          index: -1
        })} />

      <animated.div style={overlayStyle} className='absolute bottom-0 left-0 w-full'>
        <div className='p-1 bg-white bg-blur text-black rounded-b'>
          <span className='block text-sm truncate font-bold'>{book.primaryName}</span>

          {book.primaryName !== book.englishName && (
            <span className='block text-xs truncate'>{book.englishName}</span>
          )}
        </div>
      </animated.div>
    </div>
  )
}
