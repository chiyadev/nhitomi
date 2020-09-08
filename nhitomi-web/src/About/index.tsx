import React, { useMemo } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { useClientInfo, ClientInfo } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { Container } from '../Components/Container'
import { useSpring, animated } from 'react-spring'
import { HeartFilled } from '@ant-design/icons'
import { FilledButton } from '../Components/FilledButton'
import { DiscordColor, DiscordOutlined } from '../Components/Icons/DiscordOutlined'
import { FormattedMessage } from 'react-intl'
import { ScraperType } from 'nhitomi-api'
import { Anchor } from '../Components/Anchor'

export type PrefetchResult = { info: ClientInfo }
export type PrefetchOptions = {}

export const useAboutPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  const { fetchInfo } = useClientInfo()

  return {
    destination: {
      path: '/about'
    },

    fetch: async () => ({ info: await fetchInfo() })
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

const Loaded = ({ info }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.about.title'))

  const logoStyle = useSpring({
    from: { opacity: 0 },
    to: { opacity: 1 }
  })

  return (
    <Container className='divide-y divide-gray-darkest text-sm'>
      {useMemo(() => (
        <div className='p-2'>
          <div className='text-2xl'><FormattedMessage id='pages.about.title' /></div>
          <div className='text-xs text-gray-darker'><FormattedMessage id='pages.about.subtitle' /></div>
        </div>
      ), [])}

      <div className='p-2 space-y-8'>
        <div>
          <animated.img style={logoStyle} alt='logo' className='w-48 h-48 pointer-events-none select-none' src='/logo-192x192.png' />
          <br />

          <div><span className='font-bold'>nhitomi</span> — Open-source doujinshi aggregator</div>
        </div>

        {useMemo(() => (
          <Content info={info} />
        ), [info])}
      </div>
    </Container>
  )
}

const Content = ({ info }: PrefetchResult) => {
  return <>
    <div className='space-y-2'>
      <div className='text-2xl'>Features</div>

      <ul className='list-disc list-inside'>
        <li>Completele free and <Anchor target='_blank' className='font-bold' href='https://github.com/chiyadev/nhitomi'>open-source</Anchor></li>
        <li>No advertisements whatsoever</li>
        <li>Beautiful interface with first-class mobile support</li>
        <li>Customizable reader</li>
        <li>More to come...</li>
      </ul>

      <div>Missing a feature? <Anchor target='_blank' className='text-blue' href='https://github.com/chiyadev/nhitomi/issues/new'>Suggest one!</Anchor></div>
    </div>

    <div className='space-y-2'>
      <div className='text-2xl'>Accounts</div>
      <div>Registration is free and login integrates popular SSO services.</div>

      <ul className='list-disc list-inside'>
        <li>View any number of books without restrictions</li>
        <li>Create unlimited number of collections</li>
      </ul>
    </div>

    <div className='space-y-2'>
      <div className='text-2xl'>Sources</div>
      <div>Doujinshi are regularly scraped from the below sources and aggregated for convenient browsing.</div>

      <ul className='list-disc list-inside'>
        {info.scrapers.filter(s => s.type !== ScraperType.Unknown).map(scraper => (
          <li key={scraper.type}>
            <img className='inline rounded-full w-6 h-6 mr-2 align-middle' alt={scraper.type} src={`/assets/icons/${scraper.type}.jpg`} />

            {scraper.name}
            {' — '}
            <Anchor target='_blank' className='text-blue' href={scraper.url}>{scraper.url}</Anchor>
          </li>
        ))}

        <li>More to come...</li>
      </ul>
    </div>

    <div className='space-y-2'>
      <div className='text-2xl'>Discord</div>
      <div>nhitomi began its life as a Discord bot and evolved into a website after an year of development.</div>

      <ul className='list-disc list-inside'>
        <li>Read books directly in your server</li>
        <li>Detect links and display detailed information</li>
      </ul>

      <div>
        <Anchor target='_blank' href='https://discord.gg/JFNga7q'>
          <FilledButton color={DiscordColor} icon={<DiscordOutlined />}>Join our Discord server</FilledButton>
        </Anchor>
      </div>
    </div>

    <div className='space-y-2'>
      <div className='text-2xl'>Development</div>
      <div>nhitomi is developed with <HeartFilled className='text-red' /> by <Anchor target='_blank' className='font-bold' href='https://chiya.dev'>chiya.dev</Anchor>.</div>

      <ul className='list-disc list-inside'>
        <li>Codebase is moderately sized, consisting of C# and Typescript</li>
        <li>Source code is released under the permissive MIT license</li>
        <li>Contributions are accepted through pull requests</li>
      </ul>

      <br />
      <div>Want to build something custom instead?</div>

      <ul className='list-disc list-inside'>
        <li>nhitomi provides an HTTP API service, complete with <Anchor target='_blank' className='text-blue' href='https://github.com/chiyadev/nhitomi/wiki/API'>documentation</Anchor> and an <Anchor target='_blank' className='text-blue' href='/api/v1/docs.json'>OpenAPI 3.0 specification</Anchor></li>
      </ul>
    </div>

    <div>Thank you for visiting! <span className='text-gray-darker'>- chiya.dev 2018-2020</span></div>
  </>
}
