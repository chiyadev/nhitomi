import React, { ReactNode, useState, useRef, useLayoutEffect } from 'react'
import { IntlProvider, useIntl } from 'react-intl'
import { useAsync } from 'react-use'
import { useProgress } from './ProgressManager'
import { LanguageType } from 'nhitomi-api'
import { useConfig } from './ConfigManager'
import { useClient, useClientInfo } from './ClientManager'
import { AvailableLocalizations } from './Languages/languages'

export const LanguageNames: { [lang in LanguageType]: string } = {
  'ja-JP': '日本語',
  'en-US': 'English',
  'zh-CN': '中文',
  'ko-KR': '한국어',
  'it-IT': 'Italiano',
  'es-ES': 'Español',
  'de-DE': 'Deutsch',
  'fr-FR': 'français',
  'tr-TR': 'Türkçe',
  'nl-NL': 'Nederlands',
  'ru-RU': 'русский',
  'id-ID': 'Bahasa Indonesia',
  'vi-VN': 'Tiếng Việt'
}

export const AsianLanguages: LanguageType[] = [
  LanguageType.JaJP,
  LanguageType.ZhCN,
  LanguageType.KoKR
]

export function useLocalized(id: string, values?: Record<string, string | number | boolean | null | undefined | Date>): string
export function useLocalized(id: string, values?: Record<string, string | number | boolean | null | undefined | Date | ReactNode>): ReactNode

export function useLocalized(id: string, values: any) {
  const { formatMessage } = useIntl()

  return formatMessage({ id }, values)
}

export const LocaleManager = ({ children }: { children?: ReactNode }) => {
  const client = useClient()
  const { setInfo, fetchInfo } = useClientInfo()
  const [messages, setMessages] = useState<Record<string, string>>()
  const { begin, end } = useProgress()

  const [language] = useConfig('language')
  const [searchLanguages, setSearchLanguages] = useConfig('searchLanguages')
  const [, setPreferEnglishName] = useConfig('bookReaderPreferEnglishName')

  const loadId = useRef(0)

  // search languages should not be empty
  useLayoutEffect(() => {
    if (!searchLanguages.length) {
      setSearchLanguages([language])
    }
  }, [language, searchLanguages, setSearchLanguages])

  useAsync(async () => {
    const id = ++loadId.current

    begin()

    try {
      // synchronize language setting on change
      if (id > 1) {
        const info = await fetchInfo()

        if (info.authenticated) {
          setInfo({
            ...info,
            user: await client.user.updateUser({
              id: info.user.id,
              userBase: {
                ...info.user,
                language
              }
            })
          })

          setPreferEnglishName(AsianLanguages.indexOf(language) === -1)
        }
      }
    }
    catch (e) {
      console.error('could not synchronize language', e)
    }

    try {
      const messages = await loadLanguage(language)

      if (loadId.current === id) {
        setMessages(messages)

        console.log('loaded language', language, messages)
      }
    }
    catch (e) {
      console.error('could not load language', e)
    }
    finally {
      end()
    }
  }, [language])

  if (!messages)
    return null

  return (
    <IntlProvider locale={language} messages={messages} children={children} />
  )
}

async function loadLanguage(language: LanguageType): Promise<Record<string, string>> {
  let data = JSON.parse(JSON.stringify((await import('./Languages/en-US.json')).default)) // use "en-US" constant! webpack seems to break with LanguageType.EnUS string interpolation

  // layer other languages on top of the default English
  if (language !== LanguageType.EnUS && AvailableLocalizations.indexOf(language) !== -1) {
    try {
      const overlay = (await import(`./Languages/${language}.json`)).default

      data = mergeObjects(data, overlay)
    }
    catch (e) {
      console.warn('could not load language', language, e)
    }
  }

  return flattenObject(data)
}

function mergeObjects(a: { [k: string]: any }, b: { [k: string]: any }) {
  for (const key in b) {
    try {
      if (b[key].constructor === Object)
        a[key] = mergeObjects(a[key], b[key])

      else a[key] = b[key]
    }
    catch {
      a[key] = b[key]
    }
  }

  return a
}

function flattenObject(data: { [k: string]: any }): Record<string, string> {
  const flat = (res: {}, key: string, val: any, prefix = ''): {} => {
    const pre = [prefix, key].filter(v => v).join('.')

    return typeof val === 'object'
      ? Object.keys(val).reduce((prev, curr) => flat(prev, curr, val[curr], pre), res)
      : Object.assign(res, { [pre]: val })
  }

  return Object.keys(data).reduce((prev, curr) => flat(prev, curr, data[curr]), {})
}
