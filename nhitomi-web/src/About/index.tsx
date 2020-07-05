import React from 'react'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { GetInfoAuthenticatedResponse } from '../Client'
import { useTabTitleFormatted } from '../hooks'
import { PageHeader, Collapse } from 'antd'
import { InfoCircleOutlined, LinkOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { LayoutContent } from '../Layout'
import Markdown from 'markdown-to-jsx'
import { Sources } from './Sources'

type Fetched = {
  info: GetInfoAuthenticatedResponse
  readme: string
  api: string
  token: string | undefined
}

export function getAboutPrefetch(): Prefetch<Fetched> {
  return {
    path: '/about',

    func: async client => {
      const [
        info,
        readme,
        api
      ] = await Promise.all([
        client.info.getInfoAuthenticated(),
        fetch('https://raw.githubusercontent.com/chiyadev/nhitomi/master/README.md', { cache: 'no-cache' }).then(r => r.text()),
        fetch('https://raw.githubusercontent.com/chiyadev/nhitomi/master/docs/api.md', { cache: 'no-cache' }).then(r => r.text())
      ])

      client.currentInfo = { ...info, authenticated: true }

      return { info, readme, api, token: client.config.token }
    }
  }
}

export const About = () => {
  const { result } = usePrefetch(getAboutPrefetch())

  if (result)
    return <Loaded fetched={result} />

  return null
}

export const AboutLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getAboutPrefetch()} {...props} />

export const Loaded = ({ fetched: { info, readme, api, token } }: { fetched: Fetched }) => {
  useTabTitleFormatted('about.title')

  return <>
    <PageHeader
      avatar={{ icon: <InfoCircleOutlined />, shape: 'square' }}
      title={<FormattedMessage id='about.title' />}
      subTitle={<FormattedMessage id='about.sub' />} />

    <LayoutContent>
      <Collapse defaultActiveKey={['info']}>
        <Collapse.Panel
          key='info'
          header={<FormattedMessage id='about.sub' />}
          extra={<a href='https://github.com/chiyadev/nhitomi' target='_blank' rel='noopener noreferrer'><LinkOutlined /></a>}>

          <Markdown>{readme}</Markdown>
        </Collapse.Panel>

        <Collapse.Panel
          key='scrapers'
          header={<FormattedMessage id='about.sources' />}>

          <Sources sources={info.scrapers} />
        </Collapse.Panel>

        <Collapse.Panel
          key='api'
          header={<FormattedMessage id='about.api' />}
          extra={<a href='https://github.com/chiyadev/nhitomi/tree/master/docs/api.md' target='_blank' rel='noopener noreferrer'><LinkOutlined /></a>}>

          <Markdown options={{ overrides: { TokenDisplay } }}>{api.replace('{token}', token || '')}</Markdown>
        </Collapse.Panel>
      </Collapse>
    </LayoutContent>
  </>
}

const TokenDisplay = ({ children }: { children: string }) => <p>{children}</p>
