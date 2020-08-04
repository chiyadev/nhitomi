import React, { ReactNode, useState } from 'react'
import classNames from 'classnames'
import { Tooltip, TooltipProps } from './Tooltip'

export const RoundIconButton = ({ className, backColor, children, tooltip }: {
  className?: string
  backColor?: string
  children?: ReactNode
  tooltip?: TooltipProps
}) => {
  const [hover, setHover] = useState(false)

  let content = (
    <div className={classNames('w-full', 'h-full', 'flex', 'items-center', 'justify-center', backColor || 'bg-gray-100', 'transition', 'duration-75', hover ? 'bg-opacity-25' : 'bg-opacity-0')}>
      {children}
    </div>
  )

  if (tooltip) {
    content = (
      <Tooltip children={content} {...tooltip} />
    )
  }

  return (
    <div
      className={classNames('w-10', 'h-10', 'rounded-full', 'overflow-hidden', className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      children={content} />
  )
}
