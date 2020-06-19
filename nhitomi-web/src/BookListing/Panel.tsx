import React, { useContext } from 'react'
import { BookListingContext } from '.'
import { useUpdateOnEvent } from '../hooks'
import { AsyncImage } from '../AsyncImage'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { Card, Button, Typography } from 'antd'
import { CloseOutlined } from '@ant-design/icons'
import { useIntl } from 'react-intl'
import { LanguageTypeDisplay, CategoryDisplay, MaterialRatingDisplay, TagDisplay, BookTagList } from '../Tags'
import { BookReaderLink } from '../BookReader'

export const Panel = () => {
  const { formatDate, formatTime } = useIntl()

  const client = useContext(ClientContext)
  const { manager } = useContext(BookListingContext)
  const { height } = useContext(LayoutContext)

  useUpdateOnEvent(manager, 'result')

  const { book, content } = manager.result.selected || {}

  if (!book || !content)
    return null

  return (
    <Card
      style={{ position: 'relative' }}
      bordered
      key={`${book.id}/${content.id}`}
      cover={(
        <BookReaderLink id={book.id} contentId={content.id}>
          <AsyncImage
            wrapperStyle={{
              height: height * 2 / 3,
              width: '100%'
            }}
            resize='fit'
            src={() => client.book.getBookImage({ id: book.id, contentId: content.id, index: 0 })} />
        </BookReaderLink>
      )}>

      <Button
        type='text'
        size='large'
        shape='circle'
        style={{ position: 'absolute', top: 0, right: 0, margin: 2 }}
        onClick={() => manager.result = { ...manager.result, selected: undefined }}
        icon={<CloseOutlined style={{ opacity: 0.8 }} />} />

      <Card.Meta
        title={<strong>{book.primaryName}</strong>}
        description={<Typography.Text>
          {book.primaryName !== book.englishName && <p><small><strong>{book.englishName}</strong></small></p>}

          <p>
            <span>Uploaded: {formatDate(book.createdTime)} {formatTime(book.createdTime)}</span>
            <br />
            <span>Updated: {formatDate(book.updatedTime)} {formatTime(book.updatedTime)}</span>
          </p>

          <p>
            {book.contents.map(c => c.language).filter((v, i, a) => a.indexOf(v) === i).map(v => <LanguageTypeDisplay language={v} onClick={() => manager.query = { ...manager.query, language: v }} />)}

            <CategoryDisplay category={book.category} />
            <MaterialRatingDisplay rating={book.rating} />
          </p>

          <h5>Tags</h5>
          <p>
            {BookTagList.flatMap(type => book.tags[type]?.map(value =>
              <TagDisplay
                key={`${type}:${value}`}
                tag={type}
                value={value}
                onClick={() => manager.toggleTag({ type, value })} />))}
          </p>
        </Typography.Text>} />
    </Card>
  )
}
