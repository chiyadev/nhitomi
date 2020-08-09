import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { useConfig, AnimationMode } from '../ConfigManager'
import { CheckBox } from '../Components/Checkbox'

export const Animation = () => {
  return (
    <SettingsFocusContainer focus='animation'>
      <p className='text-md'><FormattedMessage id='pages.settings.appearance.animation.name' /></p>
      <p className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.animation.description' /></p>
      <br />

      <div className='space-y-2'>
        <Item mode='normal' />
        <Item mode='faster' />
        <Item mode='none' />
      </div>
    </SettingsFocusContainer>
  )
}

const Item = ({ mode }: { mode: AnimationMode }) => {
  const [value, setValue] = useConfig('animation')

  return (
    <CheckBox
      type='radio'
      value={value === mode}
      setValue={v => { if (v) setValue(mode) }}>

      <p className='text-md'><FormattedMessage id={`pages.settings.appearance.animation.${mode}.name`} /></p>
      <p className='text-xs text-gray-800'><FormattedMessage id={`pages.settings.appearance.animation.${mode}.description`} /></p>
    </CheckBox>
  )
}
