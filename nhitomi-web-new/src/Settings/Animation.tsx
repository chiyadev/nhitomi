import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'

export const Animation = () => {
  return (
    <SettingsFocusContainer focus='animation'>
      <p className='text-md'><FormattedMessage id='pages.settings.appearance.animation.name' /></p>
      <p className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.animation.description' /></p>
    </SettingsFocusContainer>
  )
}
