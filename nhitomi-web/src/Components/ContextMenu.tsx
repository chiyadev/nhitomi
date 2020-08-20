import React, { ComponentProps, useRef, useState } from 'react'
import { Dropdown, DropdownItem, DropdownDivider } from './Dropdown'
import { cx } from 'emotion'
import mergeRefs from 'react-merge-refs'
import { useShortcut } from '../shortcut'
import { CloseOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'

function getTrueBoundingRect(element: HTMLElement) {
  const rects: DOMRect[] = []
  const elementRects = element.getClientRects()

  for (let i = 0; i < elementRects.length; i++) {
    const rect = elementRects.item(i)

    if (rect)
      rects.push(rect)
  }

  // if element doesn't have bounding box (display: contents), use children boxes
  if (!rects.length) {
    for (let i = 0; i < element.children.length; i++) {
      const child = element.children.item(i)

      if (child instanceof HTMLElement) {
        const rect = getTrueBoundingRect(child)

        if (rect)
          rects.push(rect)
      }
    }
  }

  if (!rects.length) {
    return undefined
  }

  let top = Infinity
  let right = -Infinity
  let bottom = -Infinity
  let left = Infinity

  for (const rect of rects) {
    top = Math.min(top, rect.top)
    right = Math.max(right, rect.right)
    bottom = Math.max(bottom, rect.bottom)
    left = Math.min(left, rect.left)
  }

  return new DOMRect(left, top, right - left, bottom - top)
}

export const ContextMenu = ({ className, overlayClassName, moveTransition = false, offset = [0, 0], wrapperProps, overlayProps, overlay, ...props }: Omit<ComponentProps<typeof Dropdown>, 'placement' | 'trigger' | 'hideOnClick' | 'visible' | 'getReferenceClientRect'>) => {
  const wrapperRef = useRef<HTMLDivElement>(null)
  const overlayRef = useRef<HTMLDivElement>(null)

  const [visible, setVisible] = useState(false)
  const [{ x, y, trigger }, setPosition] = useState<{ x: number, y: number, trigger: ContextMenuTrigger }>({ x: 0, y: 0, trigger: 'mouse' })

  const contextProps = useContextMenu((target, trigger, { x, y }) => {
    if (trigger === 'touch' && visible)
      return

    const { left, top } = getTrueBoundingRect(target) || { left: 0, top: 0 }

    setPosition({
      x: x - left,
      y: y - top,
      trigger
    })
    setVisible(true)

    requestAnimationFrame(() => overlayRef.current?.focus())
  })

  const close = () => overlayRef.current?.blur()

  useShortcut('cancelKey', close, overlayRef)

  return (
    <Dropdown
      className={cx('display-contents', className)}
      overlayClassName={cx('select-none', overlayClassName)}
      placement='bottom-start'
      visible={visible}
      moveTransition={moveTransition}
      offset={offset}
      wrapperProps={{
        ...wrapperProps,
        ...contextProps,

        ref: wrapperProps?.ref ? mergeRefs([wrapperRef, wrapperProps.ref]) : wrapperRef
      }}
      overlayProps={{
        tabIndex: -1,
        ...overlayProps,

        ref: overlayProps?.ref ? mergeRefs([overlayRef, overlayProps.ref]) : overlayRef,

        onBlur: () => {
          setTimeout(() => {
            // hack: bring focus back to overlay if an overlay descendant stole focus
            if (overlayRef.current && overlayRef.current.contains(document.activeElement))
              overlayRef.current.focus()

            else
              setVisible(false)
          })
        }
      }}
      getReferenceClientRect={() => {
        const { left, top } = (wrapperRef.current && getTrueBoundingRect(wrapperRef.current)) || { left: 0, top: 0 }

        return {
          width: 0,
          height: 0,
          top: y + top,
          bottom: y + top,
          left: x + left,
          right: x + left
        }
      }}

      overlay={<>
        <div className='display-contents' children={overlay} />

        {trigger === 'touch' && <>
          <DropdownDivider />
          <DropdownItem icon={<CloseOutlined />} onClick={close}>
            <FormattedMessage id='components.contextMenu.close' />
          </DropdownItem>
        </>}
      </>}

      {...props} />
  )
}

type ContextMenuTrigger = 'mouse' | 'touch'

export function useContextMenu(callback: (target: HTMLDivElement, trigger: ContextMenuTrigger, position: { x: number, y: number }) => void): ComponentProps<'div'> {
  const touch = useRef<{ x: number, y: number, triggered: boolean }>()

  return {
    onContextMenu: e => {
      callback(e.currentTarget, 'mouse', { x: e.clientX, y: e.clientY })
      e.preventDefault()
    },

    onTouchStart: e => {
      if (touch.current)
        return

      touch.current = {
        x: e.touches[0].clientX,
        y: e.touches[0].clientY,
        triggered: false
      }
    },

    onTouchMove: e => {
      if (!touch.current || touch.current.triggered || e.touches.length !== 1)
        return

      const deltaX = e.touches[0].clientX - touch.current.x
      const deltaY = e.touches[0].clientY - touch.current.y

      // left swipe
      if (deltaX < -60 && Math.abs(deltaY) < Math.abs(deltaX) / 2) {
        touch.current.triggered = true

        callback(e.currentTarget, 'touch', touch.current)
      }
    },

    onTouchEnd: () => touch.current = undefined,
    onTouchCancel: () => touch.current = undefined
  }
}
