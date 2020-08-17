import React, { ReactNode, useState, MouseEvent } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { Color, getColor } from '../theme'

export const RoundIconButton = ({ className, hoverColor = getColor('white').opacity(0.25), children, onClick }: {
  className?: string
  hoverColor?: Color
  children?: ReactNode
  onClick?: (e: MouseEvent<HTMLDivElement>) => void
}) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: hoverColor.opacity(hover ? 1 : 0).rgb
  })

  return (
    <div
      className={cx('w-10 h-10 overflow-hidden cursor-pointer', className)}
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <animated.div
        style={style}
        className='w-full h-full rounded-full overflow-hidden flex items-center justify-center'
        children={children} />
    </div>
  )
}
