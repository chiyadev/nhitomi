import React from 'react'
import { useQueryState, useNavigator, NavigationArgs } from '../state'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { ClientInfo, useClientInfo } from '../ClientManager'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { stringifyOAuthState, useXsrfToken } from './oauth'
import { FormattedMessage } from 'react-intl'
import { DiscordOutlined, DiscordColor } from '../Components/Icons/DiscordOutlined'
import { FilledButton } from '../Components/FilledButton'
import { Disableable } from '../Components/Disableable'
import { TwitterOutlined, TwitterColor } from '../Components/Icons/TwitterOutlined'
import { useSpring, animated } from 'react-spring'

export type PrefetchResult = { info: ClientInfo, state: string }
export type PrefetchOptions = { redirect?: NavigationArgs }

export const useAuthenticationPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, redirect: targetRedirect }) => {
  const { info } = useClientInfo()
  const [currentState] = useQueryState<string>('replace', 'state')
  const navigator = useNavigator()

  const [token] = useXsrfToken()

  const state = (targetRedirect && stringifyOAuthState({ token, redirect: navigator.stringify(navigator.evaluate(targetRedirect)) })) || (mode === 'postfetch' && currentState) || stringifyOAuthState({ token, redirect: '/' })

  return {
    destination: {
      path: '/auth',
      query: { state }
    },

    // info is always assumed to be up-to-date
    fetch: async () => ({ info, state })
  }
}

export const AuthenticationLink = ({ redirect, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useAuthenticationPrefetch} options={{ redirect }} {...props} />
)

export const Authentication = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useAuthenticationPrefetch, options)

  if (!result)
    return null

  return (
    <Loaded {...result} />
  )
}

function appendState(url: string, state: string) {
  const u = new URL(url)
  u.searchParams.append('state', state)
  return u.href
}

const Loaded = ({ info: { discordOAuthUrl }, state }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.authentication.title'))

  const logoStyle = useSpring({
    from: { opacity: 0, transform: 'scale(0.9)' },
    to: { opacity: 1, transform: 'scale(1)' }
  })

  const infoStyle = useSpring({
    from: { opacity: 0, marginTop: -5 },
    to: { opacity: 1, marginTop: 0 }
  })

  return <>
    <animated.img style={logoStyle} alt='logo' className='w-48 h-48 pointer-events-none select-none mx-auto mb-4 mt-8' src='/logo-192x192.png' />

    <animated.div style={infoStyle}>
      <div className='space-y-1'>
        <div className='text-center text-2xl font-bold'>nhitomi</div>
        <div className='text-center text-sm text-gray-darker'><FormattedMessage id='pages.authentication.tagline' /></div>
      </div>

      <div className='mt-8 flex flex-col items-center space-y-1'>
        <a href={appendState(discordOAuthUrl, state)}>
          <FilledButton className='text-sm' color={DiscordColor} icon={<DiscordOutlined />}>
            <FormattedMessage id='pages.authentication.connect.discord' />
          </FilledButton>
        </a>

        <Disableable disabled>
          <FilledButton className='text-sm' color={TwitterColor} icon={<TwitterOutlined />}>
            <FormattedMessage id='pages.authentication.connect.twitter' />
          </FilledButton>
        </Disableable>
      </div>
    </animated.div>
  </>
}
