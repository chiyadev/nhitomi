import stringify from "json-stable-stringify";
import { NavigationLocation } from "../state";
import { atou, utoa } from "../base64";
import { useCallback } from "react";
import { randomStr } from "../random";

export type OAuthState = {
  redirect: Partial<Omit<NavigationLocation, "state">>;
  xsrf: string;
};

export function useXsrfToken(): [string, () => void] {
  let token = localStorage.getItem("xsrf");

  if (!token) {
    localStorage.setItem("xsrf", (token = randomStr(16)));
  }

  const reset = useCallback(() => localStorage.removeItem("xsrf"), []);

  return [token, reset];
}

export function stringifyOAuthState({ xsrf, redirect: { path, query, hash } }: OAuthState) {
  return utoa(stringify([xsrf, path, query, hash]));
}

export function parseOAuthState(state: string) {
  const [xsrf, path, query, hash] = JSON.parse(atou(state));

  return { xsrf, redirect: { path, query, hash } } as OAuthState;
}
