import React from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'

export type PrefetchResult = { info: ClientInfo, readme: string }
export type PrefetchOptions = {}

export const useAboutPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  const { info } = useClientInfo()

  return {
    destination: {
      path: '/about'
    },

    fetch: async () => {
      const readme = await fetch('https://raw.githubusercontent.com/chiyadev/nhitomi/master/README.md', { cache: 'no-cache' }).then(r => r.text())

      return { info, readme }
    }
  }
}

export const AboutLink = ({ ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useAboutPrefetch} options={{}} {...props} />
)

export const About = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useAboutPrefetch, { requireAuth: true, ...options })

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  )
}

const Loaded = ({ }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.about.title'))

  return null
}
