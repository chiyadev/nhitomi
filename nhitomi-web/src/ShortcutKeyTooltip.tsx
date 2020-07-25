import React, { ComponentProps, useContext } from 'react'
import { useShortcutKeyName } from './shortcuts'
import { ShortcutConfigKey } from './Client/config'
import { Tooltip } from 'antd'
import { FormattedMessage } from 'react-intl'
import { LayoutContext } from './LayoutContext'

export const ShortcutKeyTooltip = ({ shortcut, ...props }: { shortcut: ShortcutConfigKey } & Omit<ComponentProps<typeof Tooltip>, 'title'>) => {
  const { mobile } = useContext(LayoutContext)
  const key = useShortcutKeyName(shortcut)

  if (mobile)
    return <div {...props as any} />

  return (
    <Tooltip
      title={<FormattedMessage id='sidebar.pressToToggle' values={{ key }} />}
      mouseEnterDelay={0.5}
      mouseLeaveDelay={0}
      {...props} />
  )
}
