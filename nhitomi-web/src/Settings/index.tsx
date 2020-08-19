import React, { ReactNode } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { Container } from '../Components/Container'
import { FormattedMessage } from 'react-intl'
import { Language } from './Language'
import { Animation } from './Animation'
import { useScrollShortcut } from '../shortcut'
import { SettingsFocusContainer } from './common'
import { useQueryState } from '../state'
import { PictureFilled, MacCommandFilled, ReadOutlined } from '@ant-design/icons'
import { PageContainer } from '../Components/PageContainer'
import { Blur } from './Blur'
import { Shortcuts } from './Shortcuts'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { PreferEnglishName } from './PreferEnglishName'

export type PrefetchResult = ClientInfo
export type PrefetchOptions = { focus?: SettingsFocus }

export type SettingsSection = 'appearance' | 'reader' | 'keyboard'
export type SettingsItem = 'language' | 'animation' | 'blur' | 'preferEnglishName' | 'shortcuts'
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
  useTabTitle(useLocalized('pages.settings.title'))

  return (
    <Container className='divide-y divide-gray-darkest'>
      <div className='p-2'>
        <div className='text-2xl'><FormattedMessage id='pages.settings.title' /></div>
        <div className='text-xs text-gray-darker'><FormattedMessage id='pages.settings.subtitle' /></div>
      </div>

      <div className='p-2 space-y-12'>
        <Section
          type='appearance'
          name={<span><PictureFilled /> <FormattedMessage id='pages.settings.appearance.header' /></span>}>

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
      </div>
    </Container>
  )
}

const Section = ({ name, type, children, className }: { name?: ReactNode, type: SettingsSection, children?: ReactNode[], className?: string }) => (
  <SettingsFocusContainer
    focus={type}
    className={className}>

    <div className='text-xs text-gray-darker font-bold' children={name} />

    <div className='text-sm divide-y divide-gray-darkest'>
      {children?.map(child => <div className='py-4' children={child} />)}
    </div>
  </SettingsFocusContainer>
)
