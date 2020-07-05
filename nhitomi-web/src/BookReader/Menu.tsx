import React, { useMemo, useContext } from 'react'
import { Menu } from 'antd'
import { Book, BookContent, BookTag } from '../Client'
import { FileTextOutlined, ExpandAltOutlined, LinkOutlined, SearchOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { BookReaderLink } from '.'
import { useCopyToClipboard } from 'react-use'
import { NotificationContext } from '../NotificationContext'
import { BookListingLink } from '../BookListing'

export const ReaderMenu = ({ book, content, setDetails }: {
  book: Book
  content: BookContent

  setDetails: (details: boolean) => void
}) => {
  const { alert: { success } } = useContext(NotificationContext)
  const [, setClipboard] = useCopyToClipboard()

  return useMemo(() => (
    <Menu>
      <Menu.Item disabled><small>{book.primaryName}</small></Menu.Item>

      <OpenNewTabItem id={book.id} contentId={content.id} />

      <Menu.SubMenu
        icon={<SearchOutlined />}
        title={<FormattedMessage id='bookReader.menu.searchBy.title' />}>

        <SearchByItem book={book} type='name' />
        <SearchByItem book={book} type='artist' />
        <SearchByItem book={book} type='tags' />
      </Menu.SubMenu>

      <Menu.Divider />

      <Menu.Item
        icon={<LinkOutlined />}
        onClick={() => { setClipboard(book.id); success(<code>{book.id}</code>) }}>

        <FormattedMessage id='bookReader.menu.copyId' />
      </Menu.Item>

      <Menu.Item
        icon={<LinkOutlined />}
        onClick={() => { setClipboard(content.sourceUrl); success(<a href={content.sourceUrl} target='_blank' rel='noopener noreferrer'>{content.sourceUrl}</a>) }}>

        <FormattedMessage id='bookReader.menu.copySource' />
      </Menu.Item>

      <Menu.Divider />

      <Menu.Item
        icon={<FileTextOutlined />}
        onClick={() => setDetails(true)}>

        <FormattedMessage id='bookReader.menu.details' />
      </Menu.Item>
    </Menu>
  ), [book, content, setClipboard, setDetails, success])
}

const OpenNewTabItem = ({ id, contentId, ...props }: { id: string, contentId: string, [key: string]: any }) => (
  <BookReaderLink
    id={id}
    contentId={contentId}
    target='_blank'
    rel='noopener noreferrer'>

    <Menu.Item {...props} icon={<ExpandAltOutlined />}>
      <FormattedMessage id='bookReader.menu.openNewTab' />
    </Menu.Item>
  </BookReaderLink>
)

const SearchByItem = ({ book, type, ...props }: { book: Book, type: 'name' | 'artist' | 'tags', [key: string]: any }) => (
  <BookListingLink query={(
    type === 'name' ? { type: 'simple', value: book.primaryName }
      : type === 'artist' ? { type: 'tag', items: book.tags.artist?.map(value => ({ type: BookTag.Artist, value })) }
        : type === 'tags' ? { type: 'tag', items: book.tags.tag?.map(value => ({ type: BookTag.Tag, value })) }
          : undefined
  )}>
    <Menu.Item {...props}>
      <FormattedMessage id={`bookReader.menu.searchBy.${type}`} />

      <small style={{ opacity: 0.5 }}>
        {' '}
        {(
          type === 'name' ? book.primaryName
            : type === 'artist' ? book.tags.artist?.[0]
              : type === 'tags' ? `(${book.tags.tag?.length})`
                : undefined
        )}
      </small>
    </Menu.Item>
  </BookListingLink >
)
