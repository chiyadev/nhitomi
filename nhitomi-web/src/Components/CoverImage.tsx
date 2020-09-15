import React, { useState, useEffect } from 'react'
import { useSpring, animated } from 'react-spring'
import { cx } from 'emotion'
import { Loading3QuartersOutlined, WarningTwoTone } from '@ant-design/icons'
import { Tooltip } from './Tooltip'
import { FormattedMessage } from 'react-intl'
import { probeImage } from '../imageUtils'
import { getColor } from '../theme'
import { useAsync } from '../hooks'
import { getCachedImageRef, createCachedImageRef, returnCachedImageRef } from '../imageCache'

function formatAspect(x: number) {
  return `${x * 100}%`
}

export const CoverImage = ({ cacheKey, onLoad, onLoaded, className, zoomIn, autoSize, defaultAspect, sizing = 'cover' }: {
  cacheKey?: string
  onLoad: () => Promise<Blob> | Blob
  onLoaded?: (image: { url: string, width: number, height: number }) => void
  className?: string
  zoomIn?: boolean
  autoSize?: boolean
  defaultAspect?: number
  sizing?: 'cover' | 'contain'
}) => {
  const [prolongedLoad, setProlongedLoad] = useState(false) // if load is prolonged, show loading indicator

  const [loaded, setLoaded] = useState<{ url: string, width: number, height: number } | undefined>(() => {
    if (cacheKey)
      return getCachedImageRef(cacheKey)
  })

  const { loading, error } = useAsync(async () => {
    if (loaded)
      return

    const timer = window.setTimeout(() => setProlongedLoad(true), 2000)

    try {
      const blob = await onLoad()
      const { width, height } = await probeImage(blob)

      let url: string

      if (cacheKey)
        url = createCachedImageRef(cacheKey, { blob, width, height })
      else
        url = URL.createObjectURL(blob)

      const loaded = { url, width, height }

      onLoaded?.(loaded)
      setLoaded(loaded)
    }
    finally {
      clearTimeout(timer)
    }
  }, [])

  // revocation can run async
  useEffect(() => () => {
    if (loaded) {
      if (cacheKey)
        returnCachedImageRef(cacheKey)
      else
        URL.revokeObjectURL(loaded.url)
    }
  }, [cacheKey, loaded])

  const [showImage, setShowImage] = useState(!!loaded)
  const imageStyle = useSpring({
    opacity: loaded ? 1 : 0,
    transform: loaded || !zoomIn ? 'scale(1)' : 'scale(0.9)',
    onChange: {
      opacity: v => setShowImage(v > 0)
    }
  })

  const [showLoading, setShowLoading] = useState(prolongedLoad && loading)
  const loadingStyle = useSpring({
    opacity: prolongedLoad && loading ? 1 : 0,
    onChange: {
      opacity: v => setShowLoading(v > 0)
    }
  })

  const [showError, setShowError] = useState(!!error)
  const errorStyle = useSpring({
    opacity: error ? 1 : 0,
    onChange: {
      opacity: v => setShowError(v)
    }
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
      className={cx('relative select-none', className)}>

      {showImage && (
        <animated.div
          style={{
            ...imageStyle,
            backgroundImage: loaded ? `url(${loaded.url})` : undefined, // don't use emotion for perf
            backgroundSize: sizing,
            backgroundPosition: 'center',
            backgroundRepeat: 'no-repeat'
          }}
          className='absolute top-0 left-0 w-full h-full' />
      )}

      {showLoading && (
        <animated.div style={loadingStyle} className='absolute transform-center'>
          <Loading3QuartersOutlined className='animate-spin' />
        </animated.div>
      )}

      {showError && (
        <animated.div style={errorStyle} className='absolute transform-center'>
          <Tooltip
            placement='bottom'
            overlay={<>
              <div><FormattedMessage id='components.coverImage.error' /></div>
              <div><code>{error?.message || <FormattedMessage id='components.coverImage.errorUnknown' />}</code></div>
            </>}>

            <WarningTwoTone twoToneColor={getColor('red').hex} />
          </Tooltip>
        </animated.div>
      )}
    </div>
  )
}
