import React, { useState, useContext, useMemo } from 'react'
import { Affix, PageHeader } from 'antd'
import { BookOutlined } from '@ant-design/icons'
import { Search } from './Search'
import { LayoutContext } from '../LayoutContext'
import { FormattedMessage } from 'react-intl'

export const Header = () => {
  const { breakpoint } = useContext(LayoutContext)

  const [affixed, setAffixed] = useState(false)
  const [hovered, setHovered] = useState(false)

  const search = useMemo(() => <>
    <Search />
  </>, [])

  return (
    <div
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}>

      <Affix onChange={v => setAffixed(v || false)}>
        <div style={{
          background: !affixed || hovered ? 'linear-gradient(black 0%, black 100%)' : 'linear-gradient(black 0%, transparent 100%)',
          opacity: affixed && !hovered ? 0.8 : 1,
          transition: 'background 0.2s, opacity 0.2s'
        }}>
          <PageHeader
            style={{ paddingBottom: breakpoint ? 0 : undefined }}
            avatar={{ icon: <BookOutlined />, shape: 'square' }}
            title={<FormattedMessage id='bookListing.header.title' />}
            subTitle={<FormattedMessage id='bookListing.header.sub' />}
            extra={!breakpoint && search} />

          {breakpoint && (
            <div style={{ padding: '1em' }}>
              {search}
            </div>
          )}
        </div>
      </Affix>
    </div>
  )
}
