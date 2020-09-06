import { useRef, RefObject, useCallback } from 'react'
import { useKey, useKeyPress } from 'react-use'
import keycode from 'keycode'
import { ShortcutConfig, KeyModifiers, ShortcutConfigKey, useConfig } from './ConfigManager'
import { useLayout } from './LayoutManager'
import { useSpring } from 'react-spring'
import { event } from 'react-ga'

/** Returns all modifier keys pressed in the given event. */
export function getEventModifiers(e: { altKey: boolean, ctrlKey: boolean, metaKey: boolean, shiftKey: boolean }) {
  return KeyModifiers.filter(key => e[key + 'Key' as keyof typeof e])
}

export const FocusIgnoreElements: typeof HTMLElement[] = [HTMLAnchorElement]

function matchShortcut(shortcuts: ShortcutConfig[], event: KeyboardEvent, targetFocus = document.body) {
  if (FocusIgnoreElements.findIndex(e => event.target instanceof e) === -1 && event.target !== targetFocus)
    return false

  const key = event.keyCode
  const modifiers = getEventModifiers(event)

  for (const shortcut of shortcuts) {
    // match key
    if (shortcut.key !== key)
      continue

    // match either all or no modifiers
    if (shortcut.modifiers?.length) {
      if (shortcut.modifiers.length !== modifiers.length)
        continue

      for (const modifier of shortcut.modifiers) {
        if (modifiers.indexOf(modifier) === -1)
          continue
      }
    }
    else {
      if (modifiers.length)
        continue
    }

    event.preventDefault()
    return true
  }

  return false
}

/** Callback when a configured key is pressed. */
export function useShortcut(key: ShortcutConfigKey, callback: (event: KeyboardEvent) => void, ref?: RefObject<HTMLElement>) {
  const [shortcuts] = useConfig(key)

  const callback2 = useCallback((e: KeyboardEvent) => {
    callback(e)
    event({
      action: key,
      category: 'shortcut'
    })
  }, [callback, key])

  useKey(e => matchShortcut(shortcuts, e, ref?.current || undefined), callback2, {
    event: 'keydown',
    target: ref?.current || undefined
  })
}

/** Keyboard state when of a configured key. */
export function useShortcutPress(key: ShortcutConfigKey) {
  const [shortcuts] = useConfig(key)

  return useKeyPress(e => matchShortcut(shortcuts, e))
}

/** Hook to scroll window using shortcut keys. */
export function useScrollShortcut() {
  const { height } = useLayout()

  const [scrollDown] = useShortcutPress('scrollDownKey')
  const [scrollUp] = useShortcutPress('scrollUpKey')

  const timeout = useRef<number>()
  const timestamp = useRef(0)
  const direction = useRef<number>()

  useSpring({
    config: {
      bounce: 0,
      duration: 100 // slow scroll causes motion sickness...
    },
    speed: scrollDown || scrollUp ? height / 500 : 0,

    onChange: {
      speed: speed => {
        timeout.current && cancelAnimationFrame(timeout.current)

        if (speed) {
          if (!timeout.current)
            timestamp.current = performance.now()

          const dir = direction.current = scrollDown ? 1 : scrollUp ? -1 : direction.current || 0

          const frame = (time: number) => {
            const elapsed = time - timestamp.current

            window.scrollBy({ top: Math.round(elapsed * speed * dir) })

            timestamp.current = time
            timeout.current = requestAnimationFrame(frame)
          }

          timeout.current = requestAnimationFrame(frame)
        }
        else {
          timeout.current = undefined
          direction.current = undefined
        }
      }
    }
  })
}

/** Returns the key name of a shortcut key. */
export function useShortcutKeyName(key: ShortcutConfigKey) {
  const [shortcuts] = useConfig(key)

  return keycode(shortcuts[0]?.key)
}
