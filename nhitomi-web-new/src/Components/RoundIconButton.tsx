import React, { ReactNode, useState } from 'react'
import { cx } from 'emotion'

export const RoundIconButton = ({ className, backColor, children }: {
  className?: string
  backColor?: string
  children?: ReactNode
}) => {
  const [hover, setHover] = useState(false)

  return (
    <div
      className={cx('w-10 h-10 rounded-full overflow-hidden', className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <div className={cx('w-full h-full flex items-center justify-center transition duration-75', backColor || 'bg-white', hover ? 'bg-opacity-25' : 'bg-opacity-0')}>
        {children}
      </div>
    </div>
  )
}
