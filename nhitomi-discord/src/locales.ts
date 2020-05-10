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
  static default: Locale = Locale.get(LanguageType.EnUS)
  static get(language: LanguageType): Locale { return new Locale(language) }

  readonly locale: string

  protected constructor(readonly language: LanguageType) {
    this.locale = language.toString()
  }

  /** Selects a message given a message key and formats it using the given replacements. */
  get(key: string, values?: Record<string, any>): string {
    let result = __mf({ phrase: key, locale: this.locale }, values)

    // until key-level fallback is implemented, we use this hack to fallback to default language.
    // https://github.com/mashpie/i18n-node/issues/80
    // https://github.com/mashpie/i18n-node/issues/167
    if (result === key)
      result = Locale.default.get(key, values)

    return result
  }

  section(name: string): Locale {
    // eslint-disable-next-line @typescript-eslint/no-use-before-define
    return new LocaleSection(this, name + '.')
  }
}

class LocaleSection extends Locale {
  constructor(readonly inner: Locale, readonly prefix: string) { super(inner.language) }

  get(key: string, values?: Record<string, any>): string {
    return this.inner.get(this.prefix + key, values)
  }

  section(name: string): Locale {
    return new LocaleSection(this.inner, name + '.' + this.prefix)
  }
}
