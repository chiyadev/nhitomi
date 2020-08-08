import React, { useLayoutEffect } from 'react'
import { useSpring, animated } from 'react-spring'
import { useAsync } from 'react-use'

export const CoverImage = ({ onLoad, className }: { onLoad: () => Promise<Blob> | Blob, className?: string }) => {
  const { loading, error, value: loaded } = useAsync(async () => {
    const blob = await onLoad()

    return URL.createObjectURL(blob)
  }, [])

  useLayoutEffect(() => () => { loaded && URL.revokeObjectURL(loaded) }, [loaded])

  const style = useSpring(loaded
    ? {
      opacity: 1,
      transform: 'scale(1)'
    } : {
      opacity: 0,
      transform: 'scale(0.9)'
    })

  return (
    <animated.div
      style={{
        ...style,
        backgroundImage: loaded ? `url(${loaded})` : undefined,
        backgroundSize: 'cover'
      }}
      className={className} />
  )
}
