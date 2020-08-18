import React, { ComponentProps, ReactNode, useState } from 'react'
import { Tooltip } from './Tooltip'
import { useSpring, animated } from 'react-spring'
import { convertHex } from '../theme'
import { cx, css } from 'emotion'
import { RightOutlined } from '@ant-design/icons'

export const Dropdown = ({ interactive = true, placement = 'bottom-start', touch = true, padding = true, scaleTransition = true, overlayClassName, ...props }: ComponentProps<typeof Tooltip>) => {
  return (
    <Tooltip
      interactive={interactive}
      placement={placement}
      touch={touch}
      padding={false}
      scaleTransition={scaleTransition}
      overlayClassName={cx({ 'py-1': padding }, overlayClassName)}

      {...props} />
  )
}

export const DropdownItem = ({ children, className, padding = true, icon, onClick }: { children?: ReactNode, className?: string, padding?: boolean, icon?: ReactNode, onClick?: () => void }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    backgroundColor: convertHex('#fff', hover ? 0.1 : 0)
  })

  const iconStyle = useSpring({
    opacity: icon ? 1 : 0
  })

  return (
    <animated.div
      style={style}
      className={cx('cursor-pointer flex flex-row', { 'px-2 py-1': padding }, className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onClick={onClick}>

      {icon && (
        <animated.div style={iconStyle} className='w-4 text-center mr-1' children={icon} />
      )}

      <div className='flex-1 truncate' children={children} />
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

export const DropdownSubMenu = ({ name, children, ...props }: { name?: ReactNode } & ComponentProps<typeof DropdownItem>) => (
  <Dropdown
    appendTo='parent'
    overlay={children}
    placement='right-start'
    offset={[0, 3]}
    blurred={false} // 2020/08 there is a bug with Chrome that causes nested absolute backdrop-filters to not work
    moveTransition
    scaleTransition={false}>

    <DropdownItem
      children={(<>
        {name}

        <div className='ml-1 float-right h-full flex items-center'>
          <RightOutlined />
        </div>
      </>)}
      {...props} />
  </Dropdown>
)

export const DropdownDivider = ({ className }: { className?: string }) => (
  <div className={cx('mx-2 my-1 bg-gray', className, css`height: 1px; opacity: 15%;`)} />
)
