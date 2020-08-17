import React, { ReactNode, MouseEvent, useState } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { Color, getColor } from '../theme'

export const FlatButton = ({ children, color = getColor('gray', 'darkest'), icon, className, onClick }: {
  children?: ReactNode
  color?: Color
  icon?: ReactNode
  className?: string
  onClick?: (e: MouseEvent<HTMLButtonElement>) => void
}) => {
  const [focus, setFocus] = useState(false)
  const [hover, setHover] = useState(false)
  const [click, setClick] = useState(false)

  const style = useSpring({
    boxShadow: `inset 0 0 0 1px ${color.tint(click || focus || hover ? 0.25 : 0.125).rgb}`,
    backgroundColor: color.tint(click ? 0.25 : focus || hover ? 0.125 : 0).rgb
  })

  const iconStyle = useSpring({
    opacity: icon ? 1 : 0
  })

  return (
    <animated.button
      style={style}
      className={cx('text-white rounded-sm overflow-hidden cursor-pointer', className)}
      onClick={onClick}
      onFocus={() => setFocus(true)}
      onBlur={() => setFocus(false)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onMouseDown={() => setClick(true)}
      onMouseUp={() => setClick(false)}>

      <div className='px-2 py-1 flex flex-row space-x-2'>
        {icon && (
          <animated.div style={iconStyle} children={icon} />
        )}

        <div children={children} />
      </div>
    </animated.button>
  )
}
