import { PageHeader, Button, Tooltip, Dropdown } from 'antd'
import React, { useContext } from 'react'
import { useHistory } from 'react-router-dom'
import { BookReaderLink, BookReaderContext } from '.'
import { BookListingLink } from '../BookListing'
import { EllipsisOutlined } from '@ant-design/icons'
import { useShortcutKeyName } from '../shortcuts'
import { FormattedMessage, useIntl } from 'react-intl'
import { ReaderMenu } from './Menu'

export const Header = () => {
  const { formatMessage } = useIntl()
  const { book, content, setContent } = useContext(BookReaderContext)
  const { goBack } = useHistory()

  const detailsKey = useShortcutKeyName('bookReaderDetailsKey')
  const menu = ReaderMenu({ book, content, setContent })

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
        <Tooltip
          title={<FormattedMessage id='bookReader.details.pressToOpen' values={{ key: detailsKey }} />}
          placement='left'
          mouseEnterDelay={0.5}>

          <Dropdown overlay={menu}>
            <Button shape='circle' type='text'>
              <EllipsisOutlined style={{ fontSize: '1rem' }} />
            </Button>
          </Dropdown>
        </Tooltip>
      )} />
  )
}
