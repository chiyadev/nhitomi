import { RefObject, useLayoutEffect, useContext, useRef } from 'react'
import { useWindowScroll } from 'react-use'
import { LayoutContext } from '../LayoutContext'

export const ScrollPreserver = ({ containerRef }: { containerRef: RefObject<HTMLDivElement> }) => {
  const container = containerRef.current

  // rerender on scroll
  useWindowScroll()

  const layout = useContext(LayoutContext)
  const lastLayout = useRef(layout)
  const visible = useRef<Element>()
  const scrolling = useRef<number>()

  const last = lastLayout.current
  lastLayout.current = layout

  if (scrolling.current)
    return null

  if (layout.width !== last.width || layout.height !== last.height || layout.mobile !== last.mobile || layout.breakpoint !== last.breakpoint) {
    scrolling.current = requestAnimationFrame(() => {
      scrolling.current = undefined

      visible.current?.scrollIntoView({
        block: 'center',
        inline: 'center'
      })
    })
  }

  else if (container) {
    // tslint:disable-next-line: prefer-for-of
    for (let i = 0; i < container.children.length; i++) {
      const child = container.children[i]

      // child is considered visible if they're in the middle of the window
      const { top, bottom } = child.getBoundingClientRect()

      if (top < layout.height / 2 && layout.height / 2 < bottom) {
        if (visible.current !== child)
          console.log('set current', child)

        visible.current = child
        break
      }
    }
  }

  return null
}
