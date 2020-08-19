import stringify from 'json-stable-stringify'
import { useLocalStorage } from 'react-use'
import { useLayoutEffect, useCallback } from 'react'

export type OAuthState = {
  /** Redirect address. */
  redirect: string

  /** XSRF token. */
  token: string
}

const randomToken = [...Array(16)].map(() => Math.random().toString(36)[2]).join('')

export function useXsrfToken(): [string, () => void] {
  const [token, setToken] = useLocalStorage('xsrf', randomToken)

  useLayoutEffect(() => {
    if (!token)
      setToken(randomToken)
  })

  const reset = useCallback(() => setToken(randomToken), [setToken])

  return [token || randomToken, reset]
}

export function stringifyOAuthState(state: OAuthState) {
  return btoa(stringify(state))
}

export function parseOAuthState(state: string) {
  return JSON.parse(atob(state)) as OAuthState
}
