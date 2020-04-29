import { RefObject, useRef, useContext } from 'react'
import { useWindowScroll } from 'react-use'
import { LayoutResult } from './layoutManager'
import { LayoutContext } from '../LayoutContext'

export const ScrollPreserver = ({ containerRef, layout }: { containerRef: RefObject<HTMLDivElement>, layout: LayoutResult }) => {
  const container = containerRef.current

  // rerender on scroll
  useWindowScroll()

  const { height: windowHeight } = useContext(LayoutContext)
  const lastRef = useRef(layout)
  const visible = useRef<Element>()
  const scrolling = useRef<number>()

  const last = lastRef.current
  lastRef.current = layout

  if (scrolling.current)
    return null

  if (layout.width !== last.width || layout.height !== last.height) {
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

      if (top < windowHeight / 2 && windowHeight / 2 < bottom) {
        if (visible.current !== child)
          console.log('set current', child)

        visible.current = child
        break
      }
    }
  }

  return null
}
