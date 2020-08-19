import stringify from 'json-stable-stringify'
import { NavigationLocation } from '../state'
import { utoa, atou } from '../base64'

export type OAuthState = {
  redirect: Partial<Omit<NavigationLocation, 'state'>>
  xsrf: string
}

const sessionToken = [...Array(16)].map(() => Math.random().toString(36)[2]).join('')

export function useXsrfToken(reset: boolean) {
  let token = localStorage.getItem('xsrf')

  if (!token || reset) {
    localStorage.setItem('xsrf', token = sessionToken)
  }

  return token
}

export function stringifyOAuthState({ xsrf, redirect: { path, query, hash } }: OAuthState) {
  return utoa(stringify([xsrf, path, query, hash]))
}

export function parseOAuthState(state: string) {
  const [xsrf, path, query, hash] = JSON.parse(atou(state))

  return { xsrf, redirect: { path, query, hash } } as OAuthState
}
