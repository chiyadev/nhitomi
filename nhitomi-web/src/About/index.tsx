import React from 'react'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { GetInfoAuthenticatedResponse } from '../Client'
import { useTabTitleFormatted } from '../hooks'

export function getAboutPrefetch(): Prefetch<GetInfoAuthenticatedResponse> {
  return {
    path: '/about',

    func: async client => client.currentInfo = { ...await client.info.getInfoAuthenticated(), authenticated: true }
  }
}

export const About = () => {
  const { result } = usePrefetch(getAboutPrefetch())

  if (result)
    return <Loaded />

  return null
}

export const AboutLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getAboutPrefetch()} {...props} />

export const Loaded = () => {
  useTabTitleFormatted('about.title')

  return null
}
