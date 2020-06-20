import React from 'react'
import { Result, Button } from 'antd'
import { LeftOutlined } from '@ant-design/icons'
import { BookListingLink } from './BookListing'
import { FormattedMessage } from 'react-intl'
import { useLocation } from 'react-router-dom'
import { useTabTitle } from './hooks'

export const NotFound = () => {
  useTabTitle('404')

  const { pathname } = useLocation()

  return (
    <Result
      title={<FormattedMessage id='notFound.title' />}
      subTitle={<FormattedMessage id='notFound.sub' values={{ path: <code>{pathname}</code> }} />}
      icon={(
        <img
          alt='megu'
          src='/assets/statuses/megu404.jpg'
          style={{
            width: '100%',
            maxWidth: '40em',
            borderRadius: 2
          }} />
      )}
      extra={(
        <BookListingLink>
          <Button type='primary' icon={<LeftOutlined />}>
            <span><FormattedMessage id='notFound.home' /></span>
          </Button>
        </BookListingLink>
      )} />
  )
}
