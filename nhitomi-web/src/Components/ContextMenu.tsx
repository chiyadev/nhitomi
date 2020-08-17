import React, { ComponentProps, useRef, useState } from 'react'
import { Dropdown } from './Dropdown'
import { cx } from 'emotion'
import mergeRefs from 'react-merge-refs'

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

export const ContextMenu = ({ className, placement = 'right-start', moveTransition = false, offset = [0, 0], wrapperProps, overlayProps, ...props }: Omit<ComponentProps<typeof Dropdown>, 'trigger' | 'hideOnClick' | 'visible' | 'getReferenceClientRect'>) => {
  const wrapperRef = useRef<HTMLDivElement>(null)
  const overlayRef = useRef<HTMLDivElement>(null)

  const [visible, setVisible] = useState(false)
  const [{ x, y }, setPosition] = useState<{ x: number, y: number }>({ x: 0, y: 0 })

  return (
    <Dropdown
      className={cx('display-contents', className)}
      visible={visible}
      placement={placement}
      moveTransition={moveTransition}
      offset={offset}
      wrapperProps={{
        ...wrapperProps,
        ref: wrapperProps?.ref ? mergeRefs([wrapperRef, wrapperProps.ref]) : wrapperRef,
        onContextMenu: e => {
          const { left, top } = getTrueBoundingRect(e.currentTarget) || { left: 0, top: 0 }

          setPosition({
            x: e.clientX - left,
            y: e.clientY - top
          })
          setVisible(true)

          requestAnimationFrame(() => overlayRef.current?.focus())

          e.preventDefault()
          return wrapperProps?.onContextMenu?.(e)
        }
      }}
      overlayProps={{
        tabIndex: -1,
        ...overlayProps,

        ref: overlayProps?.ref ? mergeRefs([overlayRef, overlayProps.ref]) : overlayRef,
        onBlur: e => {
          setVisible(false)
          return overlayProps?.onBlur?.(e)
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
