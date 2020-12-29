import EnUS from "./en-US.json";
import JaJP from "./ja-JP.json";
import { LanguageType } from "nhitomi-api";
import flatten from "flat";
import { useCallback } from "react";
import { useIntl } from "react-intl";

export const AvailableLocalizations: LanguageType[] = [LanguageType.JaJp, LanguageType.EnUs];

export function getLocalization(language: LanguageType) {
  switch (language) {
    default:
      return EnUS;

    case LanguageType.JaJp:
      return JaJP;
  }
}

export function getFlatLocalization(language: LanguageType) {
  return flatten<any, Record<string, string>>(getLocalization(language));
}

type LocalizeFunc = (key: string, values?: Record<string, any>) => string;

export function useT(namespace?: string): LocalizeFunc {
  const { formatMessage } = useIntl();

  return useCallback(
    (key, values) => {
      if (namespace) {
        key = `${namespace}.${key}`;
      }

      return formatMessage({ id: key }, values);
    },
    [namespace, formatMessage]
  );
}
