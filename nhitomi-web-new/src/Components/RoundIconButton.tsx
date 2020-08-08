import React, { ReactNode, useState } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'

export const RoundIconButton = ({ className, backColor, children }: {
  className?: string
  backColor?: string
  children?: ReactNode
}) => {
  const [hover, setHover] = useState(false)
  const style = useSpring({ opacity: hover ? 0.25 : 0 })

  return (
    <div
      className={cx('w-10 h-10 rounded-full overflow-hidden relative', className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <div className='w-full h-full flex items-center justify-center'>
        {children}
      </div>

      <animated.span style={style} className={cx('absolute top-0 w-full h-full', backColor || 'bg-white')} />
    </div>
  )
}
