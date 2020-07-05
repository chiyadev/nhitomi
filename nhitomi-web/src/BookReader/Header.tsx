import { PageHeader, Button, Tooltip } from 'antd'
import React, { useContext } from 'react'
import { useHistory } from 'react-router-dom'
import { BookReaderLink, BookReaderContext } from '.'
import { BookListingLink } from '../BookListing'
import { EllipsisOutlined } from '@ant-design/icons'
import { useShortcutKeyName } from '../shortcuts'
import { FormattedMessage, useIntl } from 'react-intl'

export const Header = () => {
  const { formatMessage } = useIntl()
  const { book, content, setMenu } = useContext(BookReaderContext)
  const { goBack } = useHistory()

  const menuKey = useShortcutKeyName('bookReaderMenuKey')

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
          title={<FormattedMessage id='bookReader.menu.pressToOpen' values={{ key: menuKey }} />}
          placement='left'
          mouseEnterDelay={0.5}>

          <Button
            shape='circle'
            type='text'
            onClick={() => setMenu(true)}>

            <EllipsisOutlined style={{ fontSize: '1rem' }} />
          </Button>
        </Tooltip>
      )} />
  )
}
