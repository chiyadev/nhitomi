import React, { useContext } from 'react'
import { BookListingContext } from '.'
import { useUpdateOnEvent } from '../hooks'
import { AsyncImage } from '../AsyncImage'
import { ClientContext } from '../ClientContext'
import { LayoutContext } from '../LayoutContext'
import { Card, Button, Typography, Dropdown, Menu } from 'antd'
import { CloseOutlined, EditOutlined } from '@ant-design/icons'
import { LanguageTypeDisplay, CategoryDisplay, MaterialRatingDisplay, TagDisplay, BookTagList, ExpandableTag } from '../Tags'
import { BookReaderLink } from '../BookReader'
import { TimeDisplay } from '../TimeDisplay'
import { ScraperType, BookContent, LanguageType } from '../Client'
import { SourceButton } from '../SourceButton'
import { languageNames } from '../LocaleProvider'

export const Panel = () => {
  const client = useContext(ClientContext)
  const { manager } = useContext(BookListingContext)
  const { width, height } = useContext(LayoutContext)

  useUpdateOnEvent(manager, 'result')

  const { book, content } = manager.result.selected || {}

  if (!book || !content)
    return null

  return (
    <Card
      style={{ position: 'relative' }}
      bordered
      cover={(
        <BookReaderLink id={book.id} contentId={content.id}>
          <AsyncImage
            key={`${book.id}/${content.id}`}
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
        title={<>
          <BookReaderLink id={book.id} contentId={content.id}>
            <Typography.Text><strong>{book.primaryName}</strong></Typography.Text>
          </BookReaderLink>

          <Typography.Link disabled type='secondary'>
            <EditOutlined style={{
              float: 'right'
            }} />
          </Typography.Link>
        </>}
        description={<Typography.Text>
          {book.primaryName !== book.englishName && (
            <p><Typography.Text type='secondary'><small><strong>{book.englishName}</strong></small></Typography.Text></p>
          )}

          <p>
            <span>Uploaded on <TimeDisplay time={book.createdTime} /></span>
            <br />
            <span>Updated on <TimeDisplay time={book.updatedTime} /></span>
          </p>

          <p>
            {book.contents.map(c => c.language).filter((v, i, a) => a.indexOf(v) === i).map(v => {
              const tag = (
                <LanguageTypeDisplay
                  key={v}
                  language={v}
                  onClick={() => {
                    manager.result = { ...manager.result, selected: { book, content: book.contents.find(c => c.language === v) || content } }

                    requestAnimationFrame(() => manager.query = { ...manager.query, language: v })
                  }} />
              )

              if (v === content.language)
                return <span style={{ fontWeight: 'bolder' }}>{tag}</span>

              return tag
            })}

            <CategoryDisplay category={book.category} />
            <MaterialRatingDisplay rating={book.rating} />

            <ExpandableTag type='pages' value={`${content.pageCount} pages`} />
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

          <h5>View on</h5>
          <p>
            {(() => {
              const groups: { [source in ScraperType]?: { [language in LanguageType]?: BookContent[] } } = {}

              for (const content of book.contents) {
                const languages = groups[content.source] || (groups[content.source] = {})
                const contents = languages[content.language] || (languages[content.language] = [])

                contents.push(content)
              }

              return Object.values(ScraperType).map(type => {
                const group = groups[type]

                if (!group)
                  return null

                return (
                  <Dropdown key={type} overlay={(
                    <Menu>
                      {Object.values(LanguageType).map(language => {
                        const contents = group[language]

                        if (!contents)
                          return null

                        return (
                          <Menu.ItemGroup key={language} title={<small>{languageNames[language]}</small>}>
                            {contents.map(c => {
                              let text = <span>{c.sourceUrl}</span>

                              if (c === content)
                                text = <strong>{text}</strong>

                              return (
                                <Menu.Item key={c.id}>
                                  <a
                                    style={{
                                      display: 'block',
                                      overflow: 'hidden',
                                      textOverflow: 'ellipsis',
                                      maxWidth: width / 4
                                    }}
                                    target='_blank'
                                    rel='noopener noreferrer'
                                    href={c.sourceUrl}
                                    children={text} />
                                </Menu.Item>
                              )
                            })}
                          </Menu.ItemGroup>
                        )
                      })}
                    </Menu>
                  )}>

                    <SourceButton type={type} style={{ marginRight: 2 }} />
                  </Dropdown>
                )
              })
            })()}
          </p>
        </Typography.Text>} />
    </Card>
  )
}
