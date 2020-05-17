import React, { createContext, ReactNode, useState, useMemo, useContext } from 'react'
import { IntlProvider } from 'react-intl'
import { useAsync } from 'react-use'
import { ProgressContext } from './Progress'

const defaultLocale = 'en-US'

export const LocaleContext = createContext<{
  locale: string
  setLocale: (locale: string) => void
}>(undefined as any)

export const LocaleProvider = ({ children }: { children?: ReactNode }) => {
  const [locale, setLocale] = useState(defaultLocale)
  const [messages, setMessages] = useState<Record<string, string>>()
  const { start, stop } = useContext(ProgressContext)

  useAsync(async () => {
    start()

    try {
      const loaded = await loadLocale(locale)

      setMessages(loaded)
      console.log('loaded locale', locale, loaded)
    }
    finally {
      stop()
    }
  }, [locale])

  return <LocaleContext.Provider value={useMemo(() => ({
    locale,
    setLocale
  }), [
    locale,
    setLocale
  ])}>

    {messages && <IntlProvider locale={locale} messages={messages} children={children} />}
  </LocaleContext.Provider>
}

async function loadLocale(locale: string): Promise<Record<string, string>> {
  // load and clone default locale
  let obj = JSON.parse(JSON.stringify((await import(`./Locales/${defaultLocale}.json`)).default))

  // layer configured locale on top
  if (locale !== defaultLocale) {
    try {
      const obj2 = (await import(`./Locales/${locale}.json`)).default

      obj = mergeObjects(obj, obj2)
    }
    catch (e) {
      console.warn('could not load locale', locale, e)
    }
  }

  // return as flattened
  return flattenObject(obj)
}

function mergeObjects(a: { [k: string]: any }, b: { [k: string]: any }) {
  // tslint:disable-next-line: forin
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
