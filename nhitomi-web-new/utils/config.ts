import { destroyCookie, parseCookies, setCookie } from "nookies";
import { Dispatch, useCallback } from "react";

export function useCookieString(key: string): [string | undefined, Dispatch<string | undefined>] {
  const value = parseCookies()[key] as string | undefined;
  const setValue = useCallback(
    (value: string | undefined) => {
      if (typeof value === "undefined") {
        destroyCookie({}, key);
      } else {
        setCookie({}, key, value, {
          secure: true,
          sameSite: true,
        });
      }
    },
    [key]
  );

  return [value, setValue];
}
