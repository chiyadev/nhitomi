import React, { useMemo } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { Container } from '../Components/Container'
import { css, cx } from 'emotion'
import { HeartFilled } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { SupportDescription, MainCard } from './MainCard'
import { Checkout } from './Checkout'

export type PrefetchResult = { info: ClientInfo }
export type PrefetchOptions = {}

export const useSupportPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  const { fetchInfo } = useClientInfo()

  return {
    destination: {
      path: '/support'
    },

    fetch: async () => ({ info: await fetchInfo() })
  }
}

export const SupportLink = ({ ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useSupportPrefetch} options={{}} {...props} />
)

export const Support = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useSupportPrefetch, { requireAuth: true, ...options })

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  )
}

const Loaded = ({ info }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.support.title'))

  return (
    <Container className='px-2 space-y-8'>
      {useMemo(() => (
        <MainCard>
          <div className='space-y-4'>
            <div>
              <HeartFilled className={cx('text-4xl text-pink', css`transform: rotate(20deg);`)} />
              <FormattedMessage id='pages.support.subtitle' values={{
                nhitomi: (
                  <span className='ml-2 text-lg font-bold'>nhitomi</span>
                )
              }} />
            </div>

            <div className='text-gray-darker text-xs max-w-lg'>
              <SupportDescription />
            </div>
          </div>
        </MainCard>
      ), [])}

      {useMemo(() => (
        <Checkout />
      ), [])}
    </Container>
  )
}
