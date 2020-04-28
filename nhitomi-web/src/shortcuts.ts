import { useContext, useEffect, useRef } from 'react'
import { useKey, useKeyPress, useRafLoop } from 'react-use'
import { ShortcutConfig, ConfigStore, useConfig } from './Client/config'
import { LayoutContext } from './LayoutContext'
import keycode from 'keycode'

export type ModifierKey = 'alt' | 'ctrl' | 'meta' | 'shift'
export type ModifierKeyEvent = { altKey: boolean, ctrlKey: boolean, metaKey: boolean, shiftKey: boolean }

/** Returns true if any of the given modifier keys is pressed. */
export function eventHasAnyModifier(e: ModifierKeyEvent, ...keys: ModifierKey[]) {
  if (!keys.length)
    keys = ['alt', 'ctrl', 'meta', 'shift']

  return keys.some(key => e[key + 'Key' as keyof typeof e])
}

/** Returns true if all of the given modifier keys is pressed. */
export function eventHasAllModifiers(e: ModifierKeyEvent, ...keys: ModifierKey[]) {
  if (!keys.length)
    keys = ['alt', 'ctrl', 'meta', 'shift']

  return keys.every(key => e[key + 'Key' as keyof typeof e])
}

const inputIgnoreElements = [HTMLInputElement, HTMLDivElement]

function matchShortcut({ keys, modifiers }: ShortcutConfig, event: KeyboardEvent) {
  // ignore keys for some elements
  if (inputIgnoreElements.some(e => event.target instanceof e))
    return false

  // key filter
  if (!keys.includes(event.keyCode))
    return false

  // modifier filter
  if (modifiers?.length) {
    if (!eventHasAllModifiers(event, ...modifiers))
      return false
  }
  else {
    if (eventHasAnyModifier(event))
      return false
  }

  event.preventDefault()
  return true
}

export type ShortcutConfigKey = { [key in keyof ConfigStore]: ConfigStore[key] extends ShortcutConfig ? key : never }[keyof ConfigStore]

/** Callback when a configured key is pressed. */
export function useShortcut(key: ShortcutConfigKey, callback: (event: KeyboardEvent) => void) {
  const [config] = useConfig(key)

  useKey(e => matchShortcut(config, e), callback)
}

/** Keyboard state when of a configured key. */
export function useShortcutPress(key: ShortcutConfigKey) {
  const [config] = useConfig(key)

  return useKeyPress(e => matchShortcut(config, e))
}

/** Hook to scroll window using shortcut keys. */
export function useScrollShortcut() {
  const { height: windowHeight } = useContext(LayoutContext)

  const timestamp = useRef(0)
  const [scrollDown] = useShortcutPress('scrollDownKey')
  const [scrollUp] = useShortcutPress('scrollUpKey')

  const [stopScroll, , startScroll] = useRafLoop(() => {
    const now = performance.now()
    const elapsed = now - timestamp.current

    window.scrollBy({
      top: elapsed / 500 * windowHeight * (scrollDown ? 1 : -1)
    })

    timestamp.current = now
  })

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
  const [config] = useConfig(key)

  return keycode(config.keys[0])
}
