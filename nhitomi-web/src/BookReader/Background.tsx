import React from 'react'
import { useClient } from '../ClientManager'
import { CoverImage } from '../Components/CoverImage'
import { css, cx } from 'emotion'
import { useWindowScroll } from 'react-use'
import { useSpring, animated } from 'react-spring'
import { PrefetchResult } from '.'

export const Background = ({ book, content, scrollHeight }: PrefetchResult & { scrollHeight: number }) => {
  const client = useClient()
  const { y: scroll } = useWindowScroll()

  const style = useSpring({
    opacity: Math.max(0, 1 - scroll / Math.max(1, scrollHeight))
  })

  return (
    <animated.div style={style} className='fixed left-0 top-0 pointer-events-none'>
      <CoverImage
        key={`${book.id}/${content.id}`}
        className={cx('rounded overflow-hidden w-screen h-screen', css`
          z-index: -1;
          opacity: 4%;
          filter: blur(1em);
        `)}
        onLoad={async () => await client.book.getBookImage({
          id: book.id,
          contentId: content.id,
          index: 0
        })} />
    </animated.div>
  )
}
