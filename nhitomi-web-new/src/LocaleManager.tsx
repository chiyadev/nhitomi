import React, { createContext, ReactNode, useState, useMemo, useContext, useRef } from 'react'
import { IntlProvider } from 'react-intl'
import { useAsync } from 'react-use'
import { ProgressContext } from './ProgressManager'
import { LanguageType } from 'nhitomi-api'
import { useConfig } from './ConfigManager'
import { ClientContext } from './ClientManager'

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

export const LocaleContext = createContext<{
  language: LanguageType
  setLanguage: (language: LanguageType) => void
}>(undefined as any)

export const LocaleManager = ({ children }: { children?: ReactNode }) => {
  const { client, setInfo, fetchInfo } = useContext(ClientContext)
  const [language, setLanguage] = useConfig('language')
  const [messages, setMessages] = useState<Record<string, string>>()
  const { begin, end } = useContext(ProgressContext)

  const initial = useRef(true)

  useAsync(async () => {
    begin()

    try {
      if (!initial.current) {
        // synchronize language setting
        const info = await fetchInfo()

        if (info.authenticated) {
          setInfo({ ...info, user: await client.user.updateUser({ id: info.user.id, userBase: { ...info.user, language } }) })
        }
      }
      initial.current = false

      const loaded = await loadLanguage(language)

      setMessages(loaded)

      console.log('loaded language', language, loaded)
    }
    finally {
      end()
    }
  }, [language])

  return (
    <LocaleContext.Provider value={useMemo(() => ({ language, setLanguage }), [language, setLanguage])}>
      {messages && <IntlProvider locale={language} messages={messages} children={children} />}
    </LocaleContext.Provider>
  )
}

async function loadLanguage(language: string): Promise<Record<string, string>> {
  let data = JSON.parse(JSON.stringify((await import(`./Languages/${LanguageType.EnUS}.json`)).default))

  // layer other languages on top of the default English
  if (language !== LanguageType.EnUS) {
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
