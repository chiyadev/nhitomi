import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { CheckBox } from '../Components/Checkbox'
import { SupportedLocalizations } from '../Languages/languages'
import { LanguageNames } from '../LocaleManager'
import { LanguageType } from 'nhitomi-api'
import { useConfig } from '../ConfigManager'

export const Language = () => {
  const [interfaceLanguage, setInterfaceLanguage] = useConfig('language')
  const [searchLanguages, setSearchLanguages] = useConfig('searchLanguages')

  return (
    <SettingsFocusContainer focus='language'>
      <p className='text-md'><FormattedMessage id='pages.settings.appearance.language.name' /></p>
      <p className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.language.description' /></p>
      <br />

      <div>
        <div><FormattedMessage id='pages.settings.appearance.language.interface' /></div>

        {(Object.keys(LanguageNames) as LanguageType[]).filter(l => SupportedLocalizations.indexOf(l) !== -1).map(language => (
          <CheckBox
            disabled
            type='radio'
            value={language === interfaceLanguage}
            setValue={v => {
              if (v) setInterfaceLanguage(language)
            }}>

            {LanguageNames[language]}
          </CheckBox>
        ))}
      </div>
      <br />

      <div>
        <div><FormattedMessage id='pages.settings.appearance.language.search' /></div>

        {(Object.keys(LanguageNames) as LanguageType[]).map(language => (
          <CheckBox
            value={searchLanguages.indexOf(language) !== -1}
            setValue={v => {
              if (v) setSearchLanguages([...searchLanguages, language].filter((v, i, a) => a.indexOf(v) === i))
              else setSearchLanguages(searchLanguages.filter(l => l !== language))
            }}
            disabled={searchLanguages.length === 1 && searchLanguages[0] === language}>

            {LanguageNames[language]}
          </CheckBox>
        ))}
      </div>
    </SettingsFocusContainer>
  )
}
