import React, { ReactNode } from 'react'
import { useSpring, animated } from 'react-spring'
import { InboxOutlined } from '@ant-design/icons'
import { cx } from 'emotion'

export const EmptyIndicator = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const style = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 }
  })

  return (
    <animated.div
      style={style}
      className={cx('flex flex-col justify-center space-y-2', className)}>

      <InboxOutlined className='text-2xl' />

      <div children={children} />
    </animated.div>
  )
}
