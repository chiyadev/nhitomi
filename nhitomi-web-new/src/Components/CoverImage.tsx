import React, { useLayoutEffect, useState } from 'react'
import { useSpring, animated } from 'react-spring'
import { useAsync } from 'react-use'
import { cx } from 'emotion'
import { Loading3QuartersOutlined, WarningTwoTone } from '@ant-design/icons'
import { AbsoluteCenter } from './AbsoluteCenter'
import { colors } from '../theme.json'
import { Tooltip } from './Tooltip'
import { FormattedMessage } from 'react-intl'
import { getImageSize } from '../ImageUtils'

function formatAspect(x: number) {
  return `${x * 100}%`
}

export const CoverImage = ({ onLoad, className, zoomIn, autoSize, defaultAspect }: {
  onLoad: () => Promise<Blob> | Blob
  className?: string
  zoomIn?: boolean
  autoSize?: boolean
  defaultAspect?: number
}) => {
  const [prolongedLoad, setProlongedLoad] = useState(false) // if load is prolonged, show loading indicator

  const { loading, error, value: loaded } = useAsync(async () => {
    const timer = window.setTimeout(() => setProlongedLoad(true), 2000)

    try {
      const blob = await onLoad()
      const { width, height } = await getImageSize(blob)

      return { src: URL.createObjectURL(blob), width, height }
    }
    finally {
      clearTimeout(timer)
    }
  }, [])

  useLayoutEffect(() => () => { loaded && URL.revokeObjectURL(loaded.src) }, [loaded])

  const imageStyle = useSpring({
    opacity: loaded ? 1 : 0,
    transform: loaded || !zoomIn ? 'scale(1)' : 'scale(0.9)'
  })

  const loadingStyle = useSpring({
    opacity: prolongedLoad && loading ? 1 : 0
  })

  const errorStyle = useSpring({
    opacity: error ? 1 : 0
  })

  return (
    <div
      style={{
        paddingTop: loaded && autoSize
          ? formatAspect(loaded.height / loaded.width)
          : defaultAspect
            ? formatAspect(defaultAspect)
            : undefined
      }}
      className={cx('relative', className)}>

      {loaded && (
        <animated.div
          style={{
            ...imageStyle,
            backgroundImage: loaded ? `url(${loaded.src})` : undefined, // don't use emotion for perf
            backgroundSize: 'cover',
            backgroundPosition: 'center'
          }}
          className='absolute top-0 left-0 w-full h-full' />
      )}

      {prolongedLoad && (
        <animated.div style={loadingStyle} className={AbsoluteCenter}>
          <Loading3QuartersOutlined className='animate-spin' />
        </animated.div>
      )}

      {error && (
        <animated.div style={errorStyle} className={AbsoluteCenter}>
          <Tooltip
            placement='bottom'
            overlay={<>
              <div><FormattedMessage id='components.coverImage.error' /></div>
              <div><code>{error.message || <FormattedMessage id='components.coverImage.errorUnknown' />}</code></div>
            </>}>

            <WarningTwoTone twoToneColor={colors.red[500]} />
          </Tooltip>
        </animated.div>
      )}
    </div>
  )
}
