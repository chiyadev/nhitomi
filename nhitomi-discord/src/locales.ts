/* eslint-disable @typescript-eslint/no-explicit-any */
import { configure, __mf } from 'i18n'
import { LanguageType } from 'nhitomi-api'

configure({
  directory: './locales',
  defaultLocale: 'en-US',
  autoReload: true,
  objectNotation: true,
  logDebugFn: console.debug,
  logWarnFn: console.warn,
  logErrorFn: console.error
})

/** Contains localized strings that can be formatted. */
export class Locale {
  static default: Locale = new Locale(LanguageType.EnUS)
  static get(language: LanguageType): Locale { return new Locale(language) }

  readonly locale: string

  constructor(readonly language: LanguageType) {
    this.locale = language.toString()
  }

  /** Selects a message given a message key and formats it using the given replacements. */
  l(key: string, values?: Record<string, any>): string {
    let result = __mf({ phrase: key, locale: this.locale }, values)

    // until key-level fallback is implemented, we use this hack to fallback to default language.
    // https://github.com/mashpie/i18n-node/issues/80
    // https://github.com/mashpie/i18n-node/issues/167
    if (result === key)
      result = Locale.default.l(key, values)

    return result
  }
}
