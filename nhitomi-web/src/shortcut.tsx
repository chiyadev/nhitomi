import { useRef, useEffect, RefObject } from 'react'
import { useKey, useKeyPress, useRafLoop } from 'react-use'
import keycode from 'keycode'
import { ShortcutConfig, KeyModifiers, ShortcutConfigKey, useConfig } from './ConfigManager'
import { useLayout } from './LayoutManager'

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

  useKey(e => matchShortcut(shortcuts, e, ref?.current || undefined), callback, {
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

  const timestamp = useRef(0)
  const [scrollDown] = useShortcutPress('scrollDownKey')
  const [scrollUp] = useShortcutPress('scrollUpKey')

  const [stopScroll, startScroll] = useRafLoop(time => {
    const elapsed = time - timestamp.current

    window.scrollBy({
      top: elapsed / 500 * height * (scrollDown ? 1 : scrollUp ? -1 : 0)
    })

    timestamp.current = time
  })

  // cannot be useLayoutEffect
  useEffect(() => {
    timestamp.current = performance.now()

    if (scrollDown === scrollUp)
      stopScroll()
    else
      startScroll()
  }, [scrollUp, scrollDown]) // eslint-disable-line
}

/** Returns the key name of a shortcut key. */
export function useShortcutKeyName(key: ShortcutConfigKey) {
  const [shortcuts] = useConfig(key)

  return keycode(shortcuts[0]?.key)
}
