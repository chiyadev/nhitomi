import React, { useState } from 'react'
import { ShortcutConfigKeys, ShortcutConfigKey, ShortcutConfig, useConfig } from '../Client/config'
import { List, Tag, Input } from 'antd'
import keycode from 'keycode'
import { FormattedMessage } from 'react-intl'
import { getEventModifiers } from '../shortcuts'
import { PlusOutlined } from '@ant-design/icons'

export const KeySettings = ({ prefix }: { prefix: string }) => {
  return (
    <List
      size='small'
      itemLayout='horizontal'
      dataSource={ShortcutConfigKeys.filter(k => k.startsWith(prefix))}
      renderItem={key => (
        <List.Item key={key}>
          <List.Item.Meta
            title={<FormattedMessage id={`keyConfigNames.${key}`} />}
            description={<KeySettingsRow configKey={key} />} />
        </List.Item>
      )} />
  )
}

/** Converts a shortcut config to an equivalent string. */
export function ShortcutToString(shortcut: ShortcutConfig) {
  const parts: string[] = []

  for (const modifier of shortcut.modifiers || [])
    parts.push(modifier)

  parts.push(keycode(shortcut.key) || '')

  for (let i = 0; i < parts.length; i++)
    parts[i] = parts[i].charAt(0).toUpperCase() + parts[i].substring(1)

  return parts.filter((s, i, a) => a.indexOf(s) === i).join('+')
}

const KeySettingsRow = ({ configKey }: { configKey: ShortcutConfigKey }) => {
  const [shortcuts, setShortcuts] = useConfig(configKey)

  return <>
    {shortcuts.map(shortcut => (
      <Tag
        closable
        onClose={() => setShortcuts(shortcuts.filter(s => s !== shortcut))}>

        <span>{ShortcutToString(shortcut)}</span>
      </Tag>
    ))}

    <KeyInput onSubmit={s => setShortcuts([...shortcuts, s])} />
  </>
}

const KeyInput = ({ onSubmit }: { onSubmit: (shortcut: ShortcutConfig) => void }) => {
  const [enabled, setEnabled] = useState(false)
  const [value, setValue] = useState<ShortcutConfig>()

  if (!enabled)
    return (
      <Tag onClick={() => setEnabled(true)} style={{ borderStyle: 'dashed', cursor: 'pointer' }}>
        <PlusOutlined />
      </Tag>
    )

  const submit = () => {
    if (value)
      onSubmit(value)

    setEnabled(false)
    setValue(undefined)
  }

  return (
    <Input
      ref={x => x?.focus()}
      size='small'
      style={{ width: '10em' }}
      value={value ? ShortcutToString(value) : ''}
      onKeyDown={e => {
        e.preventDefault()
        e.stopPropagation()

        const key = e.keyCode
        const mods = getEventModifiers(e)

        setValue({ key, modifiers: mods.length ? mods : undefined })
      }}
      onBlur={submit} />
  )
}
