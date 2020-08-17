import React, { ReactNode, MouseEvent, useState } from 'react'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { convertHex, getColor, Color } from '../theme'

export const FilledButton = ({ children, color = getColor('gray'), icon, className, onClick }: {
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
    backgroundColor: color.hex
  })

  const overlayStyle = useSpring({
    backgroundColor: click ? convertHex('#fff', 0.25) : convertHex('#000', focus || hover ? 0.25 : 0)
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
