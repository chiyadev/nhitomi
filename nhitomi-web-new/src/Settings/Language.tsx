import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'

export const Language = () => {
  return (
    <SettingsFocusContainer focus='language'>
      <p className='text-md'><FormattedMessage id='pages.settings.appearance.language.name' /></p>
      <p className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.language.description' /></p>
    </SettingsFocusContainer>
  )
}
