import { createContext, Dispatch, SetStateAction, useCallback, useContext, useMemo } from "react";
import { LanguageType, ScraperType } from "nhitomi-api";
import { ScraperTypes } from "./constants";

export type CookieContainer = Record<string, string | undefined>;
export const CookieContext = createContext<[CookieContainer, Dispatch<SetStateAction<CookieContainer>>]>([
  {},
  () => {},
]);

export function useCookieString(key: string): [string | undefined, Dispatch<SetStateAction<string | undefined>>] {
  const [cookies, setCookies] = useContext(CookieContext);

  return [
    cookies[key],
    useCallback(
      (value) => {
        setCookies((cookies) => {
          if (typeof value === "function") {
            value = value(cookies[key]);
          }

          return {
            ...cookies,
            [key]: value,
          };
        });
      },
      [key, setCookies]
    ),
  ];
}

export type Configs = {
  token?: string;
  displayLanguage: LanguageType;
  searchLanguages: LanguageType[];
  searchSources: ScraperType[];

  bookViewportBound: boolean;
  bookLeftToRight: boolean;
  bookImagesPerRow: number;
  bookSingleCover: boolean;
};

export const DefaultConfigs: Configs = {
  token: undefined,
  displayLanguage: LanguageType.EnUS,
  searchLanguages: [LanguageType.EnUS, LanguageType.JaJP],
  searchSources: ScraperTypes,

  bookViewportBound: false,
  bookLeftToRight: false,
  bookImagesPerRow: 2,
  bookSingleCover: true,
};

function parseValueOrDefault(key: keyof Configs, value?: string): any {
  try {
    return JSON.parse(value || "");
  } catch {
    return value || DefaultConfigs[key];
  }
}

export function parseConfigs(cookies: CookieContainer): Configs {
  const result = { ...DefaultConfigs };

  for (const key in cookies) {
    (result as any)[key] = parseValueOrDefault(key as any, cookies[key]);
  }

  return result;
}

export function useConfig<TKey extends keyof Configs>(
  key: TKey
): [Configs[TKey], Dispatch<SetStateAction<Configs[TKey]>>] {
  const [value, setValue] = useCookieString(key);

  return [
    useMemo(() => parseValueOrDefault(key, value), [key, value]),
    useCallback(
      (newValue) => {
        setValue((value) => {
          if (typeof newValue === "function") {
            return JSON.stringify(newValue(parseValueOrDefault(key, value)));
          } else {
            return JSON.stringify(newValue);
          }
        });
      },
      [key]
    ),
  ];
}
