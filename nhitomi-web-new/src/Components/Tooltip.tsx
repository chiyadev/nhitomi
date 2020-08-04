import React, { ReactNode, ComponentProps } from 'react'
import Tippy from '@tippyjs/react'
import classNames from 'classnames'

import 'tippy.js/dist/tippy.css'

export type TooltipProps = ComponentProps<typeof Tooltip>

export const Tooltip = ({ className, overlay, children, animation = 'fade', hideOnClick = false, ignoreAttributes = true, touch = false, duration = 200, ...props }: {
  overlay?: ReactNode
  children?: ReactNode
} & Omit<ComponentProps<typeof Tippy>, 'content' | 'children'>) => {
  return (
    <Tippy
      content={(
        <span
          className={classNames(className, 'rounded', 'overflow-hidden')}
          children={overlay} />
      )}
      animation={animation}
      hideOnClick={hideOnClick}
      ignoreAttributes={ignoreAttributes}
      touch={touch}
      duration={duration}
      {...props}>

      <span>{children}</span>
    </Tippy>
  )
}
