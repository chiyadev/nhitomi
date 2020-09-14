import React, { ReactNode, useMemo } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { Container } from '../Components/Container'
import { FormattedMessage } from 'react-intl'
import { Language } from './Language'
import { Animation } from './Animation'
import { SettingsFocusContainer } from './SettingsFocusContainer'
import { useQueryState } from '../state'
import { MacCommandFilled, ReadOutlined, UserOutlined, PictureOutlined, ToolOutlined } from '@ant-design/icons'
import { PageContainer } from '../Components/PageContainer'
import { Blur } from './Blur'
import { Shortcuts } from './Shortcuts'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { PreferEnglishName } from './PreferEnglishName'
import { Account } from './Account'
import { Token } from './Token'
import { Debug } from './Debug'
import { Server } from './Server'
import { UserPermissions } from 'nhitomi-api'

export type PrefetchResult = ClientInfo
export type PrefetchOptions = { focus?: SettingsFocus }

export type SettingsStructure = {
  internal: {
    debug: true
    server: true
  }
  user: {
    account: true
    token: true
  }
  appearance: {
    language: true
    animation: true
    blur: true
  }
  reader: {
    preferEnglishName: true
  }
  keyboard: {
    shortcuts: true
  }
}

export type SettingsSection = keyof SettingsStructure
export type SettingsItem = { [key in SettingsSection]: keyof SettingsStructure[key] }[SettingsSection]
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
  const { result } = usePostfetch(useSettingsPrefetch, { requireAuth: true, ...options })

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded />
    </PageContainer>
  )
}

const Loaded = () => {
  const { permissions } = useClientInfo()

  useTabTitle(useLocalized('pages.settings.title'))

  return useMemo(() => (
    <Container className='divide-y divide-gray-darkest'>
      <div className='p-2'>
        <div className='text-2xl'><FormattedMessage id='pages.settings.title' /></div>
        <div className='text-sm text-gray-darker'><FormattedMessage id='pages.settings.subtitle' /></div>
      </div>

      <div className='p-2 space-y-12'>
        <Section
          type='user'
          name={<span><UserOutlined /> <FormattedMessage id='pages.settings.user.header' /></span>}>

          <Account />
          <Token />
        </Section>

        <Section
          type='appearance'
          name={<span><PictureOutlined /> <FormattedMessage id='pages.settings.appearance.header' /></span>}>

          <Language />
          <Animation />
          <Blur />
        </Section>

        <Section
          type='reader'
          name={<span><ReadOutlined /> <FormattedMessage id='pages.settings.reader.header' /></span>}>

          {[<PreferEnglishName />]}
        </Section>

        <Section
          type='keyboard'
          name={<span><MacCommandFilled /> <FormattedMessage id='pages.settings.keyboard.header' /></span>}>

          {[<Shortcuts />]}
        </Section>

        <Section
          type='internal'
          name={<span><ToolOutlined /> Internal</span>}>

          {process.env.NODE_ENV === 'development' && (
            <Debug />
          )}

          {permissions.hasPermissions(UserPermissions.ManageServer) && (
            <Server />
          )}
        </Section>
      </div>
    </Container>
  ), [permissions])
}

const Section = ({ name, type, children, className }: { name?: ReactNode, type: SettingsSection, children?: ReactNode[], className?: string }) => {
  children = children?.filter(c => c)

  if (!children?.length)
    return null

  return (
    <SettingsFocusContainer
      focus={type}
      className={className}>

      <div className='text-sm text-gray-darker font-bold' children={name} />

      <div className='divide-y divide-gray-darkest'>
        {children?.map(child => <div className='py-4' children={child} />)}
      </div>
    </SettingsFocusContainer>
  )
}
