import React, { ReactNode, useLayoutEffect, useRef, useState } from "react";
import { IntlProvider, useIntl } from "react-intl";
import { useProgress } from "./ProgressManager";
import { LanguageType } from "nhitomi-api";
import { useConfig } from "./ConfigManager";
import { useClientInfo, useClientUtils } from "./ClientManager";
import { AvailableLocalizations } from "./Languages/languages";
import { useAsync } from "./hooks";
import { captureException } from "@sentry/react";

export const LanguageNames: { [lang in LanguageType]: string } = {
  "ja-JP": "日本語",
  "en-US": "English",
  "zh-CN": "中文",
  "ko-KR": "한국어",
  "it-IT": "Italiano",
  "es-ES": "Español",
  "de-DE": "Deutsch",
  "fr-FR": "français",
  "tr-TR": "Türkçe",
  "nl-NL": "Nederlands",
  "ru-RU": "русский",
  "id-ID": "Bahasa Indonesia",
  "vi-VN": "Tiếng Việt",
};

export const CJKLanguages: LanguageType[] = [LanguageType.JaJP, LanguageType.ZhCN, LanguageType.KoKR];

export function useLocalized(
  id: string,
  values?: Record<string, string | number | boolean | null | undefined | Date>
): string;
export function useLocalized(
  id: string,
  values?: Record<string, string | number | boolean | null | undefined | Date | ReactNode>
): ReactNode;

export function useLocalized(id: string, values: any) {
  const { formatMessage } = useIntl();

  return formatMessage({ id }, values);
}

export const LocaleManager = ({ children }: { children?: ReactNode }) => {
  const {
    info: { version },
  } = useClientInfo();

  const { updateUser } = useClientUtils();
  const [messages, setMessages] = useState<Record<string, string>>();
  const { begin, end } = useProgress();

  const [language] = useConfig("language");
  const [searchLanguages, setSearchLanguages] = useConfig("searchLanguages");
  const [, setPreferEnglishName] = useConfig("bookReaderPreferEnglishName");

  const loadId = useRef(0);

  // search languages should not be empty
  useLayoutEffect(() => {
    if (!searchLanguages.length) {
      setSearchLanguages([language]);
    }
  }, [language, searchLanguages, setSearchLanguages]);

  // ensure search languages are sorted
  useLayoutEffect(() => {
    const sorted = Object.values(LanguageType).filter((l) => searchLanguages.indexOf(l) !== -1);
    let good = true;

    for (let i = 0; i < sorted.length; i++) {
      if (searchLanguages[i] !== sorted[i]) {
        good = false;
        break;
      }
    }

    if (!good) setSearchLanguages(sorted);
  }, [searchLanguages, setSearchLanguages]);

  useAsync(async () => {
    const id = ++loadId.current;

    begin();

    try {
      if (id > 1) {
        // synchronize language setting on change
        await updateUser((user) => ({ ...user, language }));

        // prefer english name for non-CJK languages
        setPreferEnglishName(CJKLanguages.indexOf(language) === -1);

        // add new interface language as search language
        setSearchLanguages([...searchLanguages, language].filter((v, i, a) => a.indexOf(v) === i));
      }
    } catch (e) {
      console.error("could not synchronize language", e);
    }

    try {
      let messages = getLanguageCached(language, version);
      if (!messages) setLanguageCached(language, version, (messages = await loadLanguage(language)));

      if (loadId.current === id) {
        setMessages(messages);
      }
    } finally {
      end();
    }
  }, [language]);

  if (!messages) return null;

  return (
    <IntlProvider locale={language} messages={messages}>
      {children}
    </IntlProvider>
  );
};

async function loadLanguage(language: LanguageType): Promise<Record<string, string>> {
  let data = JSON.parse(JSON.stringify((await import("./Languages/en-US.json")).default)); // use "en-US" constant! webpack seems to break with LanguageType.EnUS string interpolation

  // layer other languages on top of the default English
  if (language !== LanguageType.EnUS && AvailableLocalizations.indexOf(language) !== -1) {
    try {
      const overlay = (await import(`./Languages/${language}.json`)).default;

      data = mergeObjects(data, overlay);
    } catch (e) {
      console.warn("could not load language", language, e);
      captureException(e);
    }
  }

  return flattenObject(data);
}

type LocalizationCache = {
  value: Record<string, string>;
  version: string;
};

function getLanguageCached(language: LanguageType, version: string): Record<string, string> | undefined {
  // ignore cache in dev
  if (process.env.NODE_ENV === "development") return;

  try {
    const cache: Partial<LocalizationCache> = JSON.parse(localStorage.getItem(`lang_cache_${language}`) || "");

    if (typeof cache.value === "object" && cache.version === version) return cache.value;
  } catch {
    // ignored
  }
}

function setLanguageCached(language: LanguageType, version: string, messages: Record<string, string>) {
  const cache: LocalizationCache = {
    value: messages,
    version,
  };

  localStorage.setItem(`lang_cache_${language}`, JSON.stringify(cache));
}

function mergeObjects(a: { [k: string]: any }, b: { [k: string]: any }) {
  for (const key in b) {
    try {
      if (b[key].constructor === Object) a[key] = mergeObjects(a[key], b[key]);
      else a[key] = b[key];
    } catch {
      a[key] = b[key];
    }
  }

  return a;
}

function flattenObject(data: { [k: string]: any }): Record<string, string> {
  const flat = (res: {}, key: string, val: any, prefix = ""): {} => {
    const pre = [prefix, key].filter((v) => v).join(".");

    return typeof val === "object"
      ? Object.keys(val).reduce((prev, curr) => flat(prev, curr, val[curr], pre), res)
      : Object.assign(res, { [pre]: val });
  };

  return Object.keys(data).reduce((prev, curr) => flat(prev, curr, data[curr]), {});
}
