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

export class Locale {
  static readonly default = Locale.get(LanguageType.EnUs);

  static get(language: LanguageType) {
    return new Locale(language);
  }

  get isDefault() {
    return this.language === Locale.default.language;
  }

  constructor(readonly language: LanguageType) {}

  get(key: string, values?: Record<string, any>): string {
    let result = __mf({ phrase: key, locale: this.language }, values);

    // until i18n supports locale fallbacks, use this hack for fall back to default language
    // https://github.com/mashpie/i18n-node/issues/80
    // https://github.com/mashpie/i18n-node/issues/167
    if (result === key && !this.isDefault) {
      result = Locale.default.get(key, values);
    }

    return result;
  }
}
