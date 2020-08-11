import React, { ComponentProps, ReactNode, useState } from 'react'
import { Tooltip } from './Tooltip'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { cx } from 'emotion'
import { colors } from '../theme.json'

export const Dropdown = ({ interactive = true, placement = 'bottom-start', touch = true, ...props }: Omit<ComponentProps<typeof Tooltip>, 'padding'>) => {
  return (
    <Tooltip
      hideOnClick
      interactive={interactive}
      placement={placement}
      touch={touch}
      padding={false}

      {...props} />
  )
}

export const DropdownItem = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.1 : 0)
  })

  return (
    <animated.div
      className={cx('cursor-pointer px-2 py-1 truncate', className)}
      style={style}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      {children}
    </animated.div>
  )
}

export const DropdownGroup = ({ name, children, className }: { name?: ReactNode, children?: ReactNode, className?: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    color: hover ? colors.gray[500] : colors.gray[800]
  })

  return (
    <div className={cx('pl-2', className)}>
      <animated.div style={style} className='cursor-default py-1 truncate'>{name}</animated.div>

      <div
        className='rounded-l-sm overflow-hidden'
        onMouseEnter={() => setHover(true)}
        onMouseLeave={() => setHover(false)}
        children={children} />
    </div>
  )
}
