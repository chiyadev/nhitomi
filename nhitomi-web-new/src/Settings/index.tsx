import React, { ReactNode, useState } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { Container } from '../Components/Container'
import { FormattedMessage } from 'react-intl'
import { colors } from '../theme.json'
import { useSpring, animated } from 'react-spring'
import { Language } from './Language'
import { Animation } from './Animation'
import { useScrollShortcut } from '../shortcut'
import { SettingsFocusContainer } from './common'
import { useQueryState } from '../state'

export type PrefetchResult = ClientInfo
export type PrefetchOptions = { focus?: SettingsFocus }

export type SettingsSection = 'appearance' | 'keyboard'
export type SettingsItem = 'language' | 'animation'
export type SettingsFocus = SettingsSection | SettingsItem

export const useSettingsPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, focus: targetFocus }) => {
  const { fetchInfo } = useClientInfo()
  const [currentFocus] = useQueryState<SettingsFocus>('replace', 'focus')

  const focus = targetFocus || (mode === 'postfetch' && currentFocus) || undefined

  return {
    destination: {
      path: '/settings',
      query: q => ({
        ...q,
        focus
      })
    },

    restoreScroll: !focus,

    fetch: async () => {
      const info = await fetchInfo()

      if (!info.authenticated)
        throw Error('Unauthorized.')

      return info
    }
  }
}

export const SettingsLink = ({ focus, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useSettingsPrefetch} options={{ focus }} {...props} />
)

export const Settings = () => {
  const { result } = usePostfetch(useSettingsPrefetch, {})

  useScrollShortcut()

  if (!result)
    return null

  return (
    <Loaded />
  )
}

const Loaded = () => {
  return (
    <Container className='divide-y divide-gray-900'>
      <div className='p-2'>
        <p className='text-2xl'><FormattedMessage id='pages.settings.title' /></p>
        <p className='text-xs text-gray-800'><FormattedMessage id='pages.settings.subtitle' /></p>
      </div>

      <div className='p-2 space-y-8'>
        <Section type='appearance'>
          <Language />
          <Animation />
        </Section>

        <Section type='keyboard'>
          {[<div style={{ height: 10000 }} />]}
        </Section>
      </div>
    </Container>
  )
}

const Section = ({ type, children, className }: { type: SettingsSection, children?: ReactNode[], className?: string }) => {
  const [hovered, setHovered] = useState(false)
  const headerStyle = useSpring({
    color: hovered ? colors.gray[500] : colors.gray[800]
  })

  return (
    <SettingsFocusContainer
      focus={type}
      className={className}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <animated.div style={headerStyle} className='text-xs'>
        <FormattedMessage id={`pages.settings.${type}.header`} />
      </animated.div>

      <div className='text-sm divide-y divide-gray-900'>
        {children?.map(child => <div className='py-4' children={child} />)}
      </div>
    </SettingsFocusContainer>
  )
}
