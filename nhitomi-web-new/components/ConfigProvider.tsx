import React, { memo, ReactNode, SetStateAction, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { CookieContainer, CookieContext } from "../utils/config";
import { destroyCookie, setCookie } from "nookies";
import { GetInfoAuthenticatedResponse, GetInfoResponse, LanguageType } from "nhitomi-api";
import { ClientInfoContext } from "../utils/client";
import { getFlatLocalization } from "../locales";
import { IntlProvider } from "react-intl";
import { setUser } from "@sentry/minimal";

const ConfigProvider = ({
  cookies,
  info,
  children,
}: {
  cookies?: CookieContainer;
  info?: GetInfoResponse | GetInfoAuthenticatedResponse;
  children?: ReactNode;
}) => {
  useEffect(() => {
    if (info && "user" in info) {
      setUser(info.user);
    }
  }, [info]);

  return (
    <CookieProvider cookies={cookies || {}}>
      <ClientInfoProvider info={info}>{children}</ClientInfoProvider>
    </CookieProvider>
  );
};

const CookieProvider = ({ cookies, children }: { cookies: CookieContainer; children?: ReactNode }) => {
  const [value, setValueCore] = useState(cookies);
  const valueRef = useRef(cookies);

  const setValue = useCallback((value: SetStateAction<CookieContainer>) => {
    if (typeof value === "function") {
      value = value(valueRef.current);
    }

    const remainingKeys = new Set(Object.keys(valueRef.current));

    for (const key in value) {
      const current = value[key];

      if (typeof current !== "undefined") {
        remainingKeys.delete(key);

        if (valueRef.current[key] !== current) {
          setCookie(undefined, key, current, {
            path: "/",
            sameSite: "lax",
            secure: process.env.NODE_ENV !== "development",
            expires: new Date(2100, 1, 1),
          });
        }
      }
    }

    for (const key of remainingKeys) {
      destroyCookie(undefined, key);
    }

    setValueCore((valueRef.current = value));
  }, []);

  return (
    <CookieContext.Provider value={useMemo(() => [value, setValue], [value, setValue])}>
      {children}
    </CookieContext.Provider>
  );
};

const ClientInfoProvider = ({
  info,
  children,
}: {
  info?: GetInfoResponse | GetInfoAuthenticatedResponse;
  children?: ReactNode;
}) => {
  const language = info && "user" in info ? info.user.language : LanguageType.EnUs;
  const localization = useMemo(() => {
    let result = getFlatLocalization(language);

    if (language !== LanguageType.EnUs) {
      result = {
        ...getFlatLocalization(LanguageType.EnUs),
        ...result,
      };
    }

    return result;
  }, [language]);

  return (
    <ClientInfoContext.Provider value={info}>
      <IntlProvider locale={language} messages={localization}>
        {children}
      </IntlProvider>
    </ClientInfoContext.Provider>
  );
};

export default memo(ConfigProvider);
