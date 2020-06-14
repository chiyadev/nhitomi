import React, { CSSProperties, useMemo } from 'react'
import { Dropdown, Menu, Tag, Button } from 'antd'
import { LanguageType } from './Client'
import { languageNames } from './LocaleProvider'
import { FlagIcon, FlagIconCode } from 'react-flag-kit'

export function getCountryCode(lang: LanguageType) {
  return lang.split('-')[1] as FlagIconCode
}

export const LanguageSelect = ({ value, setValue, style }: {
  value: LanguageType
  setValue?: (lang: LanguageType) => void

  style?: CSSProperties
}) => {
  const menu = useMemo(() => (
    <Menu selectedKeys={[value]}>
      {Object.values(LanguageType).map(lang => {
        let label = <span> {languageNames[lang]}</span>

        if (lang === value)
          label = <strong>{label}</strong>

        return (
          <Menu.Item
            key={lang}
            icon={<FlagIcon key={lang} code={getCountryCode(lang)} />}
            onClick={() => setValue?.(lang)}>

            {label}
          </Menu.Item>
        )
      })}
    </Menu>
  ), [setValue, value])

  return (
    <Dropdown
      overlay={menu}
      placement='bottomCenter'>

      <Button icon={(
        <FlagIcon
          size={26}
          style={{ marginTop: -3 /* hack */ }}
          code={getCountryCode(value)} />
      )} />
    </Dropdown>
  )
}
