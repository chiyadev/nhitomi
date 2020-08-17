import React, { ComponentProps, ReactNode, useState } from 'react'
import { Tooltip } from './Tooltip'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { cx } from 'emotion'

export const Dropdown = ({ interactive = true, placement = 'bottom-start', touch = true, padding = true, overlayClassName, ...props }: ComponentProps<typeof Tooltip>) => {
  return (
    <Tooltip
      interactive={interactive}
      placement={placement}
      touch={touch}
      padding={false}
      overlayClassName={cx({ 'py-1': padding }, overlayClassName)}

      {...props} />
  )
}

export const DropdownItem = ({ children, className, padding = true }: { children?: ReactNode, className?: string, padding?: boolean }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.1 : 0)
  })

  return (
    <animated.div
      style={style}
      className={cx('truncate cursor-pointer', { 'px-2 py-1': padding }, className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      {children}
    </animated.div>
  )
}

export const DropdownGroup = ({ name, children, className }: { name?: ReactNode, children?: ReactNode, className?: string }) => (
  <div className={cx('pl-2', className)}>
    <div className='text-gray-darker cursor-default py-1 truncate'>{name}</div>

    <div
      className='rounded-l-sm overflow-hidden'
      children={children} />
  </div>
)
