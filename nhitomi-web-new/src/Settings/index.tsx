import React, { useMemo, Dispatch, ReactNode, useState } from 'react'
import { Prefetch, PrefetchLinkProps, PrefetchLink, usePostfetch } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { Container } from '../Components/Container'
import { FormattedMessage } from 'react-intl'
import { colors } from '../theme.json'
import { useSpring, animated } from 'react-spring'
import { useUrlState } from '../url'
import { Language } from './Language'
import { Animation } from './Animation'
import { useScrollShortcut } from '../shortcut'
import { SettingsFocusContainer } from './common'

export type SettingsSection = 'appearance' | 'keyboard'
export type SettingsItem = 'language' | 'animation'
export type SettingsFocus = SettingsSection | SettingsItem

export function getSettingsPrefetch(focus?: SettingsFocus): Prefetch<ClientInfo, { fetchInfo: () => Promise<ClientInfo>, focus?: SettingsFocus, setFocus: Dispatch<SettingsFocus | undefined> }> {
  return {
    path: '/settings',

    useData: () => {
      const { fetchInfo } = useClientInfo()
      const [currentFocus, setFocus] = useUrlState<SettingsFocus>('replace', 'focus')

      return { fetchInfo, focus: focus || currentFocus, setFocus }
    },

    fetch: async (_, __, { fetchInfo }) => {
      const info = await fetchInfo()

      if (!info.authenticated)
        throw Error('Unauthorized.')

      return info
    },

    done: (_, __, ___, { focus, setFocus }) => {
      setFocus(focus)
    }
  }
}

export const SettingsLink = ({ focus, ...props }: Omit<PrefetchLinkProps, 'fetch'> & { focus?: SettingsFocus }) => (
  <PrefetchLink fetch={getSettingsPrefetch(focus)} {...props} />
)

export const Settings = () => {
  const { result } = usePostfetch(useMemo(() => getSettingsPrefetch(), []))

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

      <div className='p-2 space-y-4'>
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
        {children?.map(child => <div className='py-2' children={child} />)}
      </div>
    </SettingsFocusContainer>
  )
}
