import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './SettingsFocusContainer'
import { useConfig } from '../ConfigManager'
import { CheckBox } from '../Components/Checkbox'

export const PreferEnglishName = () => {
  const [preferEnglishName, setPreferEnglishName] = useConfig('bookReaderPreferEnglishName')

  return (
    <SettingsFocusContainer focus='preferEnglishName'>
      <div><FormattedMessage id='pages.settings.reader.preferEnglishName.name' /></div>
      <div className='text-sm text-gray-darker'><FormattedMessage id='pages.settings.reader.preferEnglishName.description' /></div>
      <br />

      <CheckBox
        value={preferEnglishName}
        setValue={setPreferEnglishName}>

        {preferEnglishName
          ? <FormattedMessage id='pages.settings.reader.preferEnglishName.enabled' />
          : <FormattedMessage id='pages.settings.reader.preferEnglishName.disabled' />}
      </CheckBox>
    </SettingsFocusContainer>
  )
}
