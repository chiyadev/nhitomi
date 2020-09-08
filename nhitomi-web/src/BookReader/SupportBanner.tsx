import { Book, BookContent } from 'nhitomi-api'
import React, { useEffect, useState, useRef } from 'react'
import { useAsync } from 'react-use'
import { useClient } from '../ClientManager'
import { useSpring, animated } from 'react-spring'
import { HeartFilled } from '@ant-design/icons'
import { cx } from 'emotion'
import { FormattedMessage } from 'react-intl'
import { SupportLink } from '../Support'
import { usePageState } from '../state'
import { SupportDescription } from '../Support/MainCard'

export const SupportBanner = ({ book, content }: { book: Book, content: BookContent }) => {
  const client = useClient()

  const { value: thumb } = useAsync(async () => {
    const blob = await client.book.getBookImage({ id: book.id, contentId: content.id, index: 0 })

    return URL.createObjectURL(blob)
  }, [])

  // revocation can run async
  useEffect(() => thumb ? () => URL.revokeObjectURL(thumb) : undefined, [thumb])

  const style = useSpring({
    opacity: thumb ? 1 : 0
  })

  if (!thumb)
    return null

  return (
    <animated.div style={style}>
      <Inner thumb={thumb} />
    </animated.div>
  )
}

const Inner = ({ thumb }: { thumb: string }) => {
  const [hover, setHover] = useState(false)
  const [expanded, setExpanded] = usePageState('bannerExpanded', false)

  const widgetStyle = useSpring({
    backgroundColor: '#fff', // not using bg-white as it's not actually fully white
    opacity: hover || expanded ? 1 : 0.75,
    height: expanded ? 260 : 80
  })

  const [headingVisible, setHeadingVisible] = useState(hover || expanded)
  const headingStyle = useSpring({
    opacity: hover || expanded ? 1 : 0,
    marginLeft: hover || expanded ? 0 : -5,
    onChange: {
      opacity: v => setHeadingVisible(v > 0)
    }
  })

  const heartStyle = useSpring({
    transform: `rotate(${hover || expanded ? 0 : 20}deg)`
  })

  const descriptionRef = useRef<HTMLDivElement>(null)
  const descriptionStyle = useSpring({
    opacity: expanded ? 1 : 0,
    marginTop: expanded ? 5 : -5,
    height: (expanded && descriptionRef.current?.clientHeight) || 0
  })

  return (
    <div className='px-2'>
      <animated.div
        style={widgetStyle}
        className={cx('w-full max-w-xl text-black overflow-hidden rounded-lg flex flex-row mx-auto', { 'cursor-pointer': !expanded })}
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        onClick={() => setExpanded(true)}>

        <div className='relative w-1/3 select-none' style={{ maxWidth: '50%' }}>
          <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 930 1315' className='absolute transform-center w-full'>
            <image href={thumb} x={336} y={554} width={288} height={404} transform='rotate(1.6)' className='origin-center' preserveAspectRatio='xMinYMin slice' />
            <image href='/assets/images/chino_book_mask.png' />
          </svg>
        </div>

        <div className='flex-1 my-auto pr-2'>
          <div>
            <animated.div className='inline-block' style={heartStyle}><HeartFilled className='text-pink text-4xl' /></animated.div>
            <animated.span className={cx('text-base', { 'hidden': !headingVisible })} style={headingStyle}>
              <FormattedMessage id='pages.support.subtitle' values={{
                nhitomi: (
                  <span className='ml-2 text-lg font-bold'>nhitomi</span>
                )
              }} />
            </animated.span>
          </div>

          <animated.div style={descriptionStyle} className='text-xs text-gray-darker'>
            <div ref={descriptionRef} className='space-y-4'>
              <SupportDescription />

              <div className='space-x-2'>
                <SupportLink className='text-blue'>Support nhitomi!</SupportLink>
                <span className='cursor-pointer' onClick={e => { setHover(false); setExpanded(false); e.stopPropagation() }}>No thanks</span>
              </div>
            </div>
          </animated.div>
        </div>
      </animated.div>
    </div>
  )
}