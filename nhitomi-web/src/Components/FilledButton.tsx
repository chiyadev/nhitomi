import React, { ReactNode, MouseEvent, useState } from 'react'
import { colors } from '../theme.json'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'

export type ButtonType = 'default' | 'primary' | 'success' | 'error' | 'warning'

export const FilledButton = ({ children, icon, className, onClick, type = 'default' }: {
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
    case 'default': color = colors.gray[700]; break
    case 'primary': color = colors.blue[700]; break
    case 'success': color = colors.green[700]; break
    case 'error': color = colors.red[700]; break
    case 'warning': color = colors.orange[700]; break
  }

  const style = useSpring({
    backgroundColor: convertHex(color)
  })

  const overlayStyle = useSpring({
    backgroundColor: click ? convertHex('#fff', 0.25) : convertHex('#000', hover ? 0.25 : 0)
  })

  return (
    <animated.div
      style={style}
      className={cx('inline-block text-white rounded-sm overflow-hidden cursor-pointer', className)}
      tabIndex={0}
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
