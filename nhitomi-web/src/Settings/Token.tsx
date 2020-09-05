import React, { useState } from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './SettingsFocusContainer'
import { useConfig } from '../ConfigManager'
import { cx } from 'emotion'
import { NewTabLink } from '../Components/NewTabLink'
import { LinkOutlined } from '@ant-design/icons'
import { getColor } from '../theme'

export const Token = () => {
  const [token] = useConfig('token')
  const [visible, setVisible] = useState(false)

  return (
    <SettingsFocusContainer focus='token'>
      <div className='text-base'><FormattedMessage id='pages.settings.user.token.name' /></div>
      <div className='text-xs text-gray-darker'><FormattedMessage id='pages.settings.user.token.description' /></div>
      <br />

      <div className='mb-2'>
        <NewTabLink className='text-xs text-blue' href='https://github.com/chiyadev/nhitomi/wiki/API'><LinkOutlined /> <FormattedMessage id='pages.settings.user.token.docs' /></NewTabLink>
      </div>

      <div>
        <FormattedMessage id='pages.settings.user.token.token' values={{
          token: (
            <span
              onClick={() => setVisible(true)}
              className={cx('bg-gray-darkest px-1 rounded', { 'cursor-pointer': !visible })}
              style={{ color: visible ? undefined : getColor('transparent').rgb }}>

              {token}
            </span>
          )
        }} />
      </div>

      {visible && (
        <div className='text-xs text-red'><FormattedMessage id='pages.settings.user.token.warning' /></div>
      )}
    </SettingsFocusContainer>
  )
}
