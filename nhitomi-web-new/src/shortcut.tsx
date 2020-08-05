import { useContext, useRef, useEffect } from 'react'
import { useKey, useKeyPress, useRafLoop } from 'react-use'
import keycode from 'keycode'
import { ShortcutConfig, KeyModifiers, ShortcutConfigKey, useConfig } from './ConfigManager'
import { LayoutContext } from './LayoutManager'

/** Returns all modifier keys pressed in the given event. */
export function getEventModifiers(e: { altKey: boolean, ctrlKey: boolean, metaKey: boolean, shiftKey: boolean }) {
  return KeyModifiers.filter(key => e[key + 'Key' as keyof typeof e])
}

function matchShortcut(shortcuts: ShortcutConfig[], event: KeyboardEvent) {
  // ignore keys for some elements
  if ([HTMLInputElement, HTMLTextAreaElement, HTMLDivElement].some(e => event.target instanceof e))
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
export function useShortcut(key: ShortcutConfigKey, callback: (event: KeyboardEvent) => void) {
  const [shortcuts] = useConfig(key)

  useKey(e => matchShortcut(shortcuts, e), callback)
}

/** Keyboard state when of a configured key. */
export function useShortcutPress(key: ShortcutConfigKey) {
  const [shortcuts] = useConfig(key)

  return useKeyPress(e => matchShortcut(shortcuts, e))
}

/** Hook to scroll window using shortcut keys. */
export function useScrollShortcut() {
  const { height } = useContext(LayoutContext)

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
