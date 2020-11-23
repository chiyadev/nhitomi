import { destroyCookie, parseCookies, setCookie } from "nookies";
import { Dispatch, useCallback, useContext } from "react";
import { GetServerSidePropsContext } from "next";
import { RawConfigContext } from "../components/ConfigProvider";

export type RawConfig = Record<string, string | string[] | undefined>;

export function createRawConfig(ctx: Pick<GetServerSidePropsContext, "req" | "query">): RawConfig {
  const result = {
    ...parseCookies(ctx),
    ...ctx.query,
  };

  // security
  delete result.token;

  return result;
}

export function getConfigSingle(config: RawConfig, key: string) {
  const value = config[key];
  return (Array.isArray(value) ? value[0] : value) || undefined;
}

export function getConfigMultiple(config: RawConfig, key: string) {
  let value = config[key];

  if (Array.isArray(value)) {
    value = value.join(",");
  }

  return value?.split(",");
}

export function useRawConfig(): RawConfig {
  return useContext(RawConfigContext);
}

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
