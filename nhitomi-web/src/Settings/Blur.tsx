import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { useConfig, BlurSupported } from '../ConfigManager'
import { CheckBox } from '../Components/Checkbox'
import { Disableable } from '../Components/Disableable'

export const Blur = () => {
  const [blur, setBlur] = useConfig('blur')

  return (
    <SettingsFocusContainer focus='blur'>
      <div className='text-base'><FormattedMessage id='pages.settings.appearance.blur.name' /></div>
      <div className='text-xs text-gray-darker'><FormattedMessage id='pages.settings.appearance.blur.description' /></div>
      <br />

      <Disableable disabled={!BlurSupported}>
        <CheckBox
          value={blur}
          setValue={setBlur}>

          {blur
            ? <FormattedMessage id='pages.settings.appearance.blur.enabled' />
            : <FormattedMessage id='pages.settings.appearance.blur.disabled' />}
        </CheckBox>
      </Disableable>
    </SettingsFocusContainer>
  )
}
