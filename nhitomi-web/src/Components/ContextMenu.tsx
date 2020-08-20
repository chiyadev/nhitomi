import React, { ComponentProps, useRef, useState } from 'react'
import { Dropdown } from './Dropdown'
import { cx, css } from 'emotion'
import mergeRefs from 'react-merge-refs'
import { useShortcut } from '../shortcut'

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

export const ContextMenu = ({ className, overlayClassName, moveTransition = false, offset = [0, 0], wrapperProps, overlayProps, ...props }: Omit<ComponentProps<typeof Dropdown>, 'placement' | 'trigger' | 'hideOnClick' | 'visible' | 'getReferenceClientRect'>) => {
  const wrapperRef = useRef<HTMLDivElement>(null)
  const overlayRef = useRef<HTMLDivElement>(null)

  const [visible, setVisible] = useState(false)
  const [{ x, y }, setPosition] = useState<{ x: number, y: number }>({ x: 0, y: 0 })

  const contextProps = useContextMenu((target, type, { x, y }) => {
    const { left, top } = getTrueBoundingRect(target) || { left: 0, top: 0 }

    setPosition({
      x: x - left,
      y: y - top
    })
    setVisible(true)

    requestAnimationFrame(() => overlayRef.current?.focus())
  })

  useShortcut('cancelKey', () => overlayRef.current?.blur(), overlayRef)

  return (
    <Dropdown
      className={cx('display-contents', className, css`-webkit-touch-callout: none;`)}
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

      {...props} />
  )
}

export function useContextMenu(callback: (target: HTMLDivElement, type: 'mouse' | 'touch', position: { x: number, y: number }) => void): ComponentProps<'div'> {
  const touch = useRef<{
    x: number
    y: number
    timeout: number
  }>()

  const clearTouch = () => {
    if (touch.current) {
      clearTimeout(touch.current.timeout)
      touch.current = undefined
    }
  }

  return {
    onContextMenu: e => {
      callback(e.currentTarget, 'mouse', { x: e.clientX, y: e.clientY })
      e.preventDefault()
    },

    onTouchStart: e => {
      if (!touch.current) {
        const target = e.currentTarget
        const x = e.touches[0].clientX
        const y = e.touches[0].clientY

        touch.current = {
          x, y,
          timeout: window.setTimeout(() => {
            callback(target, 'touch', { x, y })
            clearTouch()
          }, 500)
        }
      }

      e.nativeEvent.returnValue = false
    },

    onTouchMove: e => {
      if (touch.current) {
        const deltaX = touch.current.x - e.touches[0].clientX
        const deltaY = touch.current.y - e.touches[0].clientY

        if (deltaX >= 10 || deltaY >= 10)
          clearTouch()
      }

      e.nativeEvent.returnValue = false
    },

    onTouchEnd: e => {
      clearTouch()
      e.nativeEvent.returnValue = false
    },

    onTouchCancel: e => {
      clearTouch()
      e.nativeEvent.returnValue = false
    }
  }
}
