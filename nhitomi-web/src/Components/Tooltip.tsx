import React, { ReactNode, ComponentProps, useState } from 'react'
import Tippy from '@tippyjs/react'
import { cx } from 'emotion'
import { animated, useSpring } from 'react-spring'

export const Tooltip = ({ className, overlay, overlayClassName, children, hideOnClick = false, ignoreAttributes = true, touch = false, duration = 200, placement = 'auto', appendTo = document.body, padding = true, moveTransition = true, scaleTransition, blurred = true, overlayProps, wrapperProps, popperOptions, ...props }: {
  overlay?: ReactNode
  overlayClassName?: string
  children?: ReactNode
  padding?: boolean
  moveTransition?: boolean
  scaleTransition?: boolean
  blurred?: boolean
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

    transform: !scaleTransition || visible ? 'scaleY(1)' : 'scaleY(0.9)',

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
          // no overflow-hidden as dropdowns can be nested
          className={cx('rounded text-sm bg-gray-darkest text-white max-w-lg', overlayClassName, overlayProps?.className, {
            'bg-blur': blurred,
            'px-2 py-1': padding
          }, {
            'origin-top': scaleTransition && placement.indexOf('bottom') !== -1,
            'origin-bottom': scaleTransition && placement.indexOf('top') !== -1
          })}>

          {overlay}
        </animated.div>
      )}
      hideOnClick={typeof props.visible !== 'undefined' ? undefined : hideOnClick} // tippy complains if hideOnClick is specified while visible controlled
      ignoreAttributes={ignoreAttributes}
      touch={touch}
      duration={duration}
      placement={placement}
      appendTo={appendTo}

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

      {...props}

      onShow={x => {
        setVisible(true)
        return props?.onShow?.(x)
      }}
      onHide={x => {
        setVisible(false)
        return props?.onHide?.(x)
      }}>

      <div {...wrapperProps} className={cx(className, wrapperProps?.className)}>{children}</div>
    </Tippy>
  )
}
