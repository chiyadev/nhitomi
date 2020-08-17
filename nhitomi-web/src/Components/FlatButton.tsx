import React, { ReactNode, MouseEvent, useState } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { convertHex, ThemeColor, getColor } from '../theme'

export const FlatButton = ({ children, color = getColor('gray'), icon, className, onClick }: {
  children?: ReactNode
  color?: ThemeColor
  icon?: ReactNode
  className?: string
  onClick?: (e: MouseEvent<HTMLButtonElement>) => void
}) => {
  const [focus, setFocus] = useState(false)
  const [hover, setHover] = useState(false)
  const [click, setClick] = useState(false)

  const style = useSpring({
    boxShadow: `inset 0 0 0 1px ${color.rgba(0.15)}`,
    backgroundColor: color.rgba(0.1)
  })

  const overlayStyle = useSpring({
    backgroundColor: convertHex('#fff', click ? 0.25 : focus || hover ? 0.125 : 0)
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

      <animated.div
        style={overlayStyle}
        className='px-2 py-1 flex flex-row space-x-2'>

        {icon && (
          <animated.div style={iconStyle} children={icon} />
        )}

        <div children={children} />
      </animated.div>
    </animated.button>
  )
}
