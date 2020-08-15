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
import { PictureFilled, MacCommandFilled } from '@ant-design/icons'
import { PageContainer } from '../Components/PageContainer'
import { Blur } from './Blur'

export type PrefetchResult = ClientInfo
export type PrefetchOptions = { focus?: SettingsFocus }

export type SettingsSection = 'appearance' | 'keyboard'
export type SettingsItem = 'language' | 'animation' | 'blur'
export type SettingsFocus = SettingsSection | SettingsItem

export const useSettingsPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, focus: targetFocus }) => {
  const { fetchInfo } = useClientInfo()
  const [currentFocus] = useQueryState<SettingsFocus>('replace', 'focus')

  const focus = targetFocus || (mode === 'postfetch' && currentFocus) || undefined

  return {
    destination: {
      path: '/settings',
      query: { focus }
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

export const Settings = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useSettingsPrefetch, options)

  useScrollShortcut()

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded />
    </PageContainer>
  )
}

const Loaded = () => {
  return (
    <Container className='divide-y divide-gray-900'>
      <div className='p-2'>
        <div className='text-2xl'><FormattedMessage id='pages.settings.title' /></div>
        <div className='text-xs text-gray-800'><FormattedMessage id='pages.settings.subtitle' /></div>
      </div>

      <div className='p-2 space-y-8'>
        <Section
          type='appearance'
          name={<span><PictureFilled /> <FormattedMessage id='pages.settings.appearance.header' /></span>}>

          <Language />
          <Animation />
          <Blur />
        </Section>

        <Section
          type='keyboard'
          name={<span><MacCommandFilled /> <FormattedMessage id='pages.settings.keyboard.header' /></span>}>

          {[<div style={{ height: 10000 }} />]}
        </Section>
      </div>
    </Container>
  )
}

const Section = ({ name, type, children, className }: { name?: ReactNode, type: SettingsSection, children?: ReactNode[], className?: string }) => {
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

      <animated.div style={headerStyle} className='text-xs' children={name} />

      <div className='text-sm divide-y divide-gray-900'>
        {children?.map(child => <div className='py-4' children={child} />)}
      </div>
    </SettingsFocusContainer>
  )
}
