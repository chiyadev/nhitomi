import React, { ReactNode, MouseEvent, useState } from 'react'
import { colors } from '../theme.json'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { ButtonType } from './FilledButton'

export const FlatButton = ({ children, icon, className, onClick, type = 'default' }: {
  children?: ReactNode
  icon?: ReactNode
  className?: string
  onClick?: (e: MouseEvent<HTMLDivElement>) => void
  type?: ButtonType
}) => {
  const [hover, setHover] = useState(false)
  const [click, setClick] = useState(false)

  let color: string

  switch (type) {
    case 'default': color = colors.gray[500]; break
    case 'primary': color = colors.blue[500]; break
    case 'success': color = colors.green[500]; break
    case 'error': color = colors.red[500]; break
    case 'warning': color = colors.orange[500]; break
  }

  const style = useSpring({
    boxShadow: `inset 0 0 0 1px ${convertHex(color, 0.15)}`,
    backgroundColor: convertHex(color, 0.1)
  })

  const overlayStyle = useSpring({
    backgroundColor: convertHex('#fff', click ? 0.25 : hover ? 0.125 : 0)
  })

  return (
    <animated.div
      style={style}
      className={cx('inline-block text-white rounded-sm overflow-hidden cursor-pointer', className)}
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onMouseDown={() => setClick(true)}
      onMouseUp={() => setClick(false)}>

      <animated.div
        style={overlayStyle}
        className='px-2 py-1 space-x-1'>

        {icon}
        <div className='inline-block' children={children} />
      </animated.div>
    </animated.div>
  )
}
