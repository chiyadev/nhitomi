import React, { useMemo, useContext } from 'react'
import { Menu } from 'antd'
import { Book, BookContent, BookTag } from '../Client'
import { FileTextOutlined, ExpandAltOutlined, LinkOutlined, SearchOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { BookReaderLink } from '.'
import { useCopyToClipboard } from 'react-use'
import { NotificationContext } from '../NotificationContext'
import { BookListingLink } from '../BookListing'
import { SearchQuery } from '../BookListing/searchManager'

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
        <SearchByItem book={book} type='character' />
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

const SearchByItem = ({ book, type, ...props }: { book: Book, type: 'name' | 'artist' | 'character' | 'tags', [key: string]: any }) => {
  let query: Partial<SearchQuery>
  let hint: string

  const createHint = (tags: string[]) => tags.length <= 2 ? tags.join(', ') : `(${tags.length})`

  switch (type) {
    case 'name':
      query = { type: 'simple', value: book.primaryName }
      hint = book.primaryName
      break

    case 'artist':
      if (!book.tags.artist?.length)
        return null

      query = { type: 'tag', items: book.tags.artist.map(value => ({ type: BookTag.Artist, value })) }
      hint = createHint(book.tags.artist)
      break

    case 'character':
      if (!book.tags.character?.length)
        return null

      query = { type: 'tag', items: book.tags.character.map(value => ({ type: BookTag.Character, value })) }
      hint = createHint(book.tags.character)
      break

    case 'tags':
      if (!book.tags.tag?.length)
        return null

      query = { type: 'tag', items: book.tags.tag.map(value => ({ type: BookTag.Tag, value })) }
      hint = createHint(book.tags.tag)
      break
  }

  return (
    <BookListingLink query={query}>
      <Menu.Item {...props}>
        <FormattedMessage id={`bookReader.menu.searchBy.${type}`} />

        <small style={{ opacity: 0.5 }}> {hint}</small>
      </Menu.Item>
    </BookListingLink >
  )
}
