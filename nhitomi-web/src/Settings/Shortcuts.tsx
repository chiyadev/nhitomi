import React, { useState, ReactNode } from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './SettingsFocusContainer'
import { useConfig, ShortcutConfigKey, ShortcutConfigKeys, ShortcutConfig } from '../ConfigManager'
import keycode from 'keycode'
import { useSpring, animated } from 'react-spring'
import { getColor } from '../theme'
import { CloseOutlined, PlusOutlined } from '@ant-design/icons'
import { cx } from 'emotion'
import { getEventModifiers } from '../shortcut'
import { useLocalized } from '../LocaleManager'
import { Input } from '../Components/Input'

export const Shortcuts = () => {
  const generalKeys: ShortcutConfigKey[] = []
  const bookReaderKeys: ShortcutConfigKey[] = []

  for (const key of ShortcutConfigKeys) {
    if (key.startsWith('bookReader'))
      bookReaderKeys.push(key)
    else
      generalKeys.push(key)
  }

  return (
    <SettingsFocusContainer focus='shortcuts'>
      <div><FormattedMessage id='pages.settings.keyboard.shortcuts.name' /></div>
      <div className='text-sm text-gray-darker'><FormattedMessage id='pages.settings.keyboard.shortcuts.description' /></div>
      <br />

      <div className='divide-y divide-gray-darkest'>
        <div className='pb-4 space-y-1'>
          <div><FormattedMessage id='pages.settings.keyboard.shortcuts.general.name' /></div>
          {generalKeys.map(key => (
            <Shortcut section='general' shortcutKey={key} />
          ))}
        </div>

        <div className='pt-4 space-y-1'>
          <div><FormattedMessage id='pages.settings.keyboard.shortcuts.bookReader.name' /></div>
          {bookReaderKeys.map(key => (
            <Shortcut section='bookReader' shortcutKey={key} />
          ))}
        </div>
      </div>
    </SettingsFocusContainer>
  )
}

function stringifyShortcut(shortcut: Partial<ShortcutConfig>) {
  const parts: string[] = []

  for (const modifier of shortcut.modifiers || [])
    parts.push(modifier)

  if (shortcut.key)
    parts.push(keycode(shortcut.key))

  return parts
    .filter((v, i, a) => a.indexOf(v) === i)
    .map(v => v.substring(0, 1).toUpperCase() + v.substring(1)) // capitalize
    .join('+')
}

const Shortcut = ({ section, shortcutKey }: { section: 'general' | 'bookReader', shortcutKey: ShortcutConfigKey }) => {
  const [shortcuts, setShortcuts] = useConfig(shortcutKey)

  return (
    <div className='space-x-1 ml-4'>
      <span><FormattedMessage id={`pages.settings.keyboard.shortcuts.${section}.${shortcutKey}`} /> </span>

      {shortcuts.map(shortcut => (
        <ItemPart>
          <span className='align-middle'>{stringifyShortcut(shortcut)}</span>

          <CloseOutlined className='ml-1 text-gray-darker cursor-pointer' onClick={() => setShortcuts(shortcuts.filter(s => s !== shortcut))} />
        </ItemPart>
      ))}

      <ItemNew onAdd={s => setShortcuts([...shortcuts, s])} />
    </div>
  )
}

const ItemPart = ({ children, className, onClick }: { children?: ReactNode, className?: string, onClick?: () => void }) => {
  const [hover, setHover] = useState(false)
  const color = getColor('gray', 'darkest').opacity(0.5)

  const style = useSpring({
    boxShadow: `inset 0 0 0 1px ${color.tint(hover ? 0.25 : 0.125).rgb}`,
    backgroundColor: color.tint(hover ? 0.25 : 0).rgb
  })

  return (
    <animated.div
      style={style}
      className={cx('inline-block align-middle text-sm px-1 rounded overflow-hidden cursor-default', className)}
      onClick={onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      children={children} />
  )
}

const ItemNew = ({ onAdd }: { onAdd?: (shortcut: ShortcutConfig) => void }) => {
  const [current, setCurrent] = useState<Partial<ShortcutConfig>>()
  const placeholder = useLocalized('pages.settings.keyboard.shortcuts.enterKey')

  if (!current) {
    return (
      <ItemPart className='cursor-pointer' onClick={() => setCurrent({})}>
        <PlusOutlined className='text-gray-darker' />
      </ItemPart>
    )
  }

  return (
    <Input
      autoFocus
      className='text-sm rounded overflow-hidden w-32'
      value={stringifyShortcut(current)}
      placeholder={placeholder}
      onKeyDown={e => {
        const key = e.keyCode
        const modifiers = getEventModifiers(e)

        setCurrent({ key, modifiers })

        e.preventDefault()
      }}
      onBlur={() => {
        const { key, modifiers } = current

        if (key)
          onAdd?.({ key, modifiers })

        setCurrent(undefined)
      }} />
  )
}
