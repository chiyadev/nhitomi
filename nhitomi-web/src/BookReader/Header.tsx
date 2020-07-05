import { PageHeader, Button, Dropdown } from 'antd'
import React, { useContext } from 'react'
import { useHistory } from 'react-router-dom'
import { BookReaderLink, BookReaderContext } from '.'
import { BookListingLink } from '../BookListing'
import { EllipsisOutlined } from '@ant-design/icons'
import { useIntl } from 'react-intl'
import { ReaderMenu } from './Menu'

export const Header = ({ setDetails }: {
  setDetails: (details: boolean) => void
}) => {
  const { formatMessage } = useIntl()
  const { book, content } = useContext(BookReaderContext)
  const { goBack } = useHistory()

  const menu = ReaderMenu({ book, content, setDetails })

  return (
    <PageHeader
      onBack={goBack}
      title={book.primaryName}
      subTitle={book.englishName}
      breadcrumb={{
        routes: [{
          path: 'listing',
          breadcrumbName: formatMessage({ id: 'bookListing.header.title' })
        }, {
          path: 'book',
          breadcrumbName: book.primaryName
        }],
        itemRender: ({ path, breadcrumbName }) => {
          switch (path) {
            case 'listing': return <BookListingLink>{breadcrumbName}</BookListingLink>
            case 'book': return <BookReaderLink id={book.id} contentId={content.id}>{breadcrumbName}</BookReaderLink>
          }
        }
      }}
      extra={(
        <Dropdown overlay={menu}>
          <Button shape='circle' type='text'>
            <EllipsisOutlined style={{ fontSize: '1rem' }} />
          </Button>
        </Dropdown>
      )} />
  )
}
