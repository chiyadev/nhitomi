import React from 'react'
import { FormattedMessage } from 'react-intl'
import { SettingsFocusContainer } from './common'
import { CheckBox } from '../Components/Checkbox'
import { AvailableLocalizations } from '../Languages/languages'
import { LanguageNames } from '../LocaleManager'
import { LanguageType } from 'nhitomi-api'
import { useConfig } from '../ConfigManager'
import { LocaleFlag } from '../Components/LocaleFlag'

export const Language = () => {
  const [interfaceLanguage, setInterfaceLanguage] = useConfig('language')
  const [searchLanguages, setSearchLanguages] = useConfig('searchLanguages')

  return (
    <SettingsFocusContainer focus='language'>
      <div className='text-base'><FormattedMessage id='pages.settings.appearance.language.name' /></div>
      <div className='text-xs text-gray-800'><FormattedMessage id='pages.settings.appearance.language.description' /></div>
      <br />

      <div>
        <div><FormattedMessage id='pages.settings.appearance.language.interface' /></div>

        {(Object.keys(LanguageNames) as LanguageType[]).filter(l => AvailableLocalizations.indexOf(l) !== -1).map(language => (
          <CheckBox
            type='radio'
            value={language === interfaceLanguage}
            setValue={v => {
              if (v) setInterfaceLanguage(language)
            }}>

            <span><LocaleFlag language={language} size={20} /> {LanguageNames[language]}</span>
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

            <span><LocaleFlag language={language} size={20} /> {LanguageNames[language]}</span>
          </CheckBox>
        ))}
      </div>
    </SettingsFocusContainer>
  )
}
