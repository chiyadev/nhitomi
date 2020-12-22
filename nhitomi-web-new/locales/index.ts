import EnUS from "./en-US.json";
import JaJP from "./ja-JP.json";
import { LanguageType } from "nhitomi-api";
import flatten from "flat";
import { useCallback } from "react";
import { useIntl } from "react-intl";

export const AvailableLocalizations: LanguageType[] = [LanguageType.JaJP, LanguageType.EnUS];

export function getLocalization(language: LanguageType) {
  switch (language) {
    default:
      return EnUS;

    case LanguageType.JaJP:
      return JaJP;
  }
}

export function getFlatLocalization(language: LanguageType) {
  return flatten<any, Record<string, string>>(getLocalization(language));
}

type LocalizeFunc = (key: string, values?: Record<string, any>) => string;

export function useT(): LocalizeFunc {
  const { formatMessage } = useIntl();

  return useCallback((key, values) => formatMessage({ id: key }, values), [formatMessage]);
}
