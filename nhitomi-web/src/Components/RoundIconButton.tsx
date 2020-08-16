import React, { ReactNode, useState } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'

export const RoundIconButton = ({ className, backColor, children }: {
  className?: string
  backColor?: string
  children?: ReactNode
}) => {
  const [hover, setHover] = useState(false)
  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.25 : 0)
  })

  return (
    <div
      className='w-10 h-10 overflow-hidden'
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <animated.div
        style={style}
        className={cx('w-full h-full rounded-full overflow-hidden flex items-center justify-center', className)}
        children={children} />
    </div>
  )
}
