import React, { Dispatch } from 'react'
import { useQueryState, useNavigator, NavigationArgs } from '../state'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { ClientInfo, useClientInfo } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { Container } from '../Components/Container'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'

export type PrefetchResult = ClientInfo
export type PrefetchOptions = { state?: string, redirect?: NavigationArgs }

export const useAuthenticationPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, state: targetState, redirect: targetRedirect }) => {
  const { fetchInfo } = useClientInfo()
  const [currentState] = useQueryState<string>('replace', 'state')
  const navigator = useNavigator()

  const state = targetState || (targetRedirect && navigator.stringify(navigator.evaluate(targetRedirect))) || (mode === 'postfetch' && currentState) || undefined

  return {
    destination: {
      path: '/auth',
      query: { state }
    },

    fetch: fetchInfo
  }
}

export const AuthenticationLink = ({ redirect, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useAuthenticationPrefetch} options={{ redirect }} {...props} />
)

export const Authentication = (options: PrefetchOptions) => {
  const { result, setResult } = usePostfetch(useAuthenticationPrefetch, options)

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded result={result} setResult={setResult} />
    </PageContainer>
  )
}

const Loaded = ({ result, setResult }: { result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  useTabTitle(useLocalized('pages.authentication.title'))

  return (
    <Container>
      login page
    </Container>
  )
}
