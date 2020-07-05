import React, { useContext } from 'react'
import { PageHeader } from 'antd'
import { BookOutlined } from '@ant-design/icons'
import { Search } from './Search'
import { LayoutContext } from '../LayoutContext'
import { FormattedMessage } from 'react-intl'
import { AffixGradientPageHeader } from './AffixGradientPageHeader'

export const Header = () => {
  const { breakpoint } = useContext(LayoutContext)

  const search = <Search />

  return (
    <AffixGradientPageHeader>
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
    </AffixGradientPageHeader>
  )
}
