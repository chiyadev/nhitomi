import React, { ComponentProps } from 'react'
import { LanguageType } from 'nhitomi-api'
import { FlagIcon, FlagIconCode } from 'react-flag-kit'
import { useLocale } from '../LocaleManager'

export const LocaleFlag = ({ language, className, size }: { language: LanguageType, className?: string, size?: number }) => {
  return (
    <FlagIcon code={language.split('-')[1] as FlagIconCode} className={className} size={size} />
  )
}

export const CurrentLocaleFlag = (props: Omit<ComponentProps<typeof LocaleFlag>, 'language'>) => {
  const { language } = useLocale()

  return (
    <LocaleFlag language={language} {...props} />
  )
}
