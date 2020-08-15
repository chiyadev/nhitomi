import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { useConfig } from '../ConfigManager'
import { CheckBox } from '../Components/Checkbox'

export const Blur = () => {
  const [blur, setBlur] = useConfig('blur')

  return (
    <SettingsFocusContainer focus='blur'>
      <div className='text-base'><FormattedMessage id='pages.settings.appearance.blur.name' /></div>
      <div className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.blur.description' /></div>
      <br />

      <CheckBox
        value={blur}
        setValue={setBlur}>

        {blur
          ? <FormattedMessage id='pages.settings.appearance.blur.enabled' />
          : <FormattedMessage id='pages.settings.appearance.blur.disabled' />}
      </CheckBox>
    </SettingsFocusContainer>
  )
}
