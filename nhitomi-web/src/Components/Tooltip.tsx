import React, { ReactNode, ComponentProps, useState } from 'react'
import Tippy from '@tippyjs/react'
import { cx } from 'emotion'
import { animated, useSpring } from 'react-spring'

export const Tooltip = ({ className, overlay, children, hideOnClick = false, ignoreAttributes = true, touch = false, duration = 200, placement = 'auto', padding = true, moveTransition = true, overlayProps, wrapperProps, onShow, onHide, popperOptions, ...props }: {
  overlay?: ReactNode
  children?: ReactNode
  padding?: boolean
  moveTransition?: boolean
  overlayProps?: Omit<ComponentProps<'div'>, 'ref'> & Pick<ComponentProps<typeof animated.div>, 'ref'>
  wrapperProps?: ComponentProps<'div'>
} & Omit<ComponentProps<typeof Tippy>, 'content' | 'render' | 'moveTransition' | 'children' | 'animation' | 'arrow'>) => {
  const [visible, setVisible] = useState(false)
  const [render, setRender] = useState(false)

  const style = useSpring({
    opacity: visible ? 1 : 0,

    marginTop: moveTransition && placement.indexOf('bottom') !== -1 && !visible ? -5 : 0,
    marginRight: moveTransition && placement.indexOf('left') !== -1 && !visible ? -5 : 0,
    marginBottom: moveTransition && placement.indexOf('top') !== -1 && !visible ? -5 : 0,
    marginLeft: moveTransition && placement.indexOf('right') !== -1 && !visible ? -5 : 0,

    onChange: {
      opacity: v => setRender(v > 0)
    }
  })

  return (
    <Tippy
      render={props => !render ? <span /> : (
        <animated.div
          {...props}
          {...overlayProps}

          style={{
            ...style,
            ...overlayProps?.style
          }}
          className={cx('rounded overflow-hidden text-xs bg-gray-900 bg-blur text-white', { 'px-2 py-1': padding }, overlayProps?.className)}>

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
