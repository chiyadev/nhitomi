import React, { memo, ReactNode, SetStateAction, useCallback, useMemo, useRef, useState } from "react";
import { CookieContainer, CookieContext } from "../utils/config";
import { destroyCookie, setCookie } from "nookies";

const ConfigProvider = ({ cookies, children }: { cookies: CookieContainer; children?: ReactNode }) => {
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
            secure: window.location.protocol === "https:",
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

export default memo(ConfigProvider);
