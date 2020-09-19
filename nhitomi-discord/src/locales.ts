/* eslint-disable @typescript-eslint/no-explicit-any */
import { __mf, configure } from "i18n";
import { LanguageType } from "nhitomi-api";

configure({
  directory: "./Locales",
  defaultLocale: "en-US",
  autoReload: true,
  objectNotation: true,
  logDebugFn: console.debug,
  logWarnFn: console.warn,
  logErrorFn: console.error,
  updateFiles: false,
});

/** Contains localized strings that can be formatted. */
export class Locale {
  static default: Locale = Locale.get(LanguageType.EnUS);

  static get(language: LanguageType): Locale {
    return new Locale(language);
  }

  get default(): boolean {
    return this.language === Locale.default.language;
  }

  constructor(readonly language: LanguageType) {}

  /** Selects a message given a message key and formats it using the given replacements. */
  get(key: string, values?: Record<string, any>): string {
    let result = __mf({ phrase: key, locale: this.language }, values);

    // until key-level fallback is implemented, we use this hack for fallback to default language.
    // https://github.com/mashpie/i18n-node/issues/80
    // https://github.com/mashpie/i18n-node/issues/167
    if (result === key && !this.default)
      result = Locale.default.get(key, values);

    return result;
  }
}
