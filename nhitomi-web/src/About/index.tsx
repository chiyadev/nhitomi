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
}

export function getAboutPrefetch(): Prefetch<Fetched> {
  return {
    path: '/about',

    func: async client => {
      const info = await client.info.getInfoAuthenticated()
      const readme = await fetch('https://raw.githubusercontent.com/chiyadev/nhitomi/master/README.md', { cache: 'no-cache' }).then(r => r.text())

      client.currentInfo = { ...info, authenticated: true }

      return { info, readme }
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

export const Loaded = ({ fetched }: {
  fetched: Fetched
}) => {
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

          <Markdown>{fetched.readme}</Markdown>
        </Collapse.Panel>

        <Collapse.Panel
          key='scrapers'
          header={<FormattedMessage id='about.sources' />}>

          <Sources sources={fetched.info.scrapers} />
        </Collapse.Panel>
      </Collapse>
    </LayoutContent>
  </>
}
