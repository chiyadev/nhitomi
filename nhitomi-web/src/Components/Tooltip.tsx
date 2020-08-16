import React, { ReactNode, ComponentProps, useState } from 'react'
import Tippy from '@tippyjs/react'
import { cx } from 'emotion'
import { animated, useSpring } from 'react-spring'

export const Tooltip = ({ className, overlay, children, hideOnClick = false, ignoreAttributes = true, touch = false, duration = 200, placement = 'auto', padding = true, wrapperProps, onShow, onHide, popperOptions, ...props }: {
  overlay?: ReactNode
  children?: ReactNode
  padding?: boolean
  wrapperProps?: ComponentProps<'div'>
} & Omit<ComponentProps<typeof Tippy>, 'content' | 'render' | 'children' | 'animation' | 'arrow'>) => {
  const [visible, setVisible] = useState(false)
  const [render, setRender] = useState(false)

  const style = useSpring({
    opacity: visible ? 1 : 0,
    marginTop: placement.indexOf('bottom') === -1 ? 0 : visible ? 0 : -5,
    marginRight: placement.indexOf('left') === -1 ? 0 : visible ? 0 : -5,
    marginBottom: placement.indexOf('top') === -1 ? 0 : visible ? 0 : -5,
    marginLeft: placement.indexOf('right') === -1 ? 0 : visible ? 0 : -5,
    onChange: {
      opacity: v => setRender(v > 0)
    }
  })

  return (
    <Tippy
      render={props => !render ? <span /> : (
        <animated.div
          {...props}
          style={style}
          className={cx('rounded overflow-hidden text-xs bg-gray-900 bg-blur text-white', { 'px-2 py-1': padding })}>

          {overlay}
        </animated.div>
      )}
      hideOnClick={hideOnClick}
      ignoreAttributes={ignoreAttributes}
      touch={touch}
      duration={duration}
      placement={placement}

      onShow={x => {
        setVisible(true)
        return onShow?.(x)
      }}
      onHide={x => {
        setVisible(false)
        return onHide?.(x)
      }}

      popperOptions={{
        modifiers: [
          {
            // disable scroll listeners for performance if not visible
            name: 'eventListeners',
            enabled: true,
            phase: 'write',
            options: {
              scroll: render,
              resize: render
            }
          },
          ...(popperOptions?.modifiers || [])
        ],
        ...popperOptions
      }}

      {...props}>

      <div {...wrapperProps} className={cx(className, wrapperProps?.className)}>{children}</div>
    </Tippy>
  )
}
