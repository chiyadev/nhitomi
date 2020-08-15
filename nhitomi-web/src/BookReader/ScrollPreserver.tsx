import { RefObject, useRef } from 'react'
import { useWindowScroll } from 'react-use'
import { isSafari, safariResizeDelay } from '../fuckSafari'
import { useLayout } from '../LayoutManager'
import { LayoutResult } from './layoutEngine'

export const ScrollPreserver = ({ containerRef, layout }: { containerRef: RefObject<HTMLDivElement>, layout: LayoutResult }) => {
  const container = containerRef.current

  // rerender on scroll
  useWindowScroll()

  const lastLayoutRef = useRef(layout)
  const lastLayout = lastLayoutRef.current

  const { height } = useLayout()
  const visible = useRef<Element>()
  const scrolling = useRef<number>()

  if (!scrolling.current) {
    if (layout.cause !== 'images' && (layout.width !== lastLayout.width || layout.height !== lastLayout.height)) {
      const scroll = () => {
        visible.current?.scrollIntoView({
          block: 'center',
          inline: 'center'
        })
        scrolling.current = undefined
      }

      if (isSafari)
        scrolling.current = window.setTimeout(scroll, safariResizeDelay)
      else
        scrolling.current = requestAnimationFrame(scroll)
    }

    else if (container) {
      // tslint:disable-next-line: prefer-for-of
      for (let i = 0; i < container.children.length; i++) {
        const child = container.children[i]

        // child is considered visible if they're in the middle of the window
        const { top, bottom } = child.getBoundingClientRect()

        if (top < height / 2 && height / 2 < bottom) {
          visible.current = child
          break
        }
      }
    }
  }

  lastLayoutRef.current = layout

  return null
}
