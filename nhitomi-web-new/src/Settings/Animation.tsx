import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { useConfig, AnimationMode } from '../ConfigManager'
import { CheckBox } from '../Components/Checkbox'

export const Animation = () => {
  return (
    <SettingsFocusContainer focus='animation'>
      <div className='text-base'><FormattedMessage id='pages.settings.appearance.animation.name' /></div>
      <div className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.animation.description' /></div>
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

      <div className='text-sm'><FormattedMessage id={`pages.settings.appearance.animation.${mode}.name`} /></div>
      <div className='text-xs text-gray-800'><FormattedMessage id={`pages.settings.appearance.animation.${mode}.description`} /></div>
    </CheckBox>
  )
}
