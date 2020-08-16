import React, { useState } from 'react'
import { BookTag, ScraperType } from 'nhitomi-api'
import { useClient, useClientInfo } from '../ClientManager'
import { CoverImage } from '../Components/CoverImage'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { useLayout } from '../LayoutManager'
import { UploadOutlined, HistoryOutlined, LinkOutlined, ReadOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { TimeDisplay } from '../Components/TimeDisplay'
import { BookTagColors } from '../Components/colors'
import { convertHex } from '../theme'
import { Dropdown, DropdownItem, DropdownGroup } from '../Components/Dropdown'
import { colors } from '../theme.json'
import { PrefetchResult, BookReaderLink } from '.'
import { BookTags, ScraperTypes, LanguageTypes } from '../orderedConstants'
import { LanguageNames } from '../LocaleManager'
import { NewTabLink } from '../Components/NewTabLink'
import { BookListingLink } from '../BookListing'
import { Disableable } from '../Components/Disableable'

export const Info = ({ book, content }: PrefetchResult) => {
  const client = useClient()
  const { screen } = useLayout()

  return (
    <div className={cx('flex p-4', screen === 'sm' ? 'flex-col space-y-4' : 'flex-row space-x-4')}>
      <div className={cx(screen === 'sm' ? 'flex-1' : 'w-1/4')}>
        <CoverImage
          autoSize
          defaultAspect={7 / 5}
          className='rounded overflow-hidden'
          onLoad={async () => await client.book.getBookImage({
            id: book.id,
            contentId: content.id,
            index: 0
          })} />
      </div>

      <div className='flex-1 space-y-4'>
        <div>
          <div className='text-2xl font-bold'>{book.primaryName}</div>
          <div className='text-sm font-bold text-gray-800'>{book.englishName}</div>
        </div>

        <div className='space-y-2'>
          {BookTags.map(tag => {
            const tags = book.tags[tag]

            if (!tags)
              return null

            return (
              <div>
                <div className='text-xs text-gray-800 mb-1'><FormattedMessage id={`types.bookTag.${tag}`} /></div>
                <div className='text-sm leading-tight'>
                  {tags.sort().map(value => (
                    <BookListingLink query={{ query: `${tag}:${value.replace(/\s/g, '_')}` }}>
                      <Tag type={tag} value={value} />
                    </BookListingLink>
                  ))}
                </div>
              </div>
            )
          })}

          <div>
            <div className='text-xs text-gray-800 mb-1'><FormattedMessage id='pages.bookReader.sources' /></div>
            <div className='space-x-1'>
              {ScraperTypes.map(type => {
                const sourceContents = book.contents.filter(c => c.source === type).sort((a, b) => b.id.localeCompare(a.id))

                if (!sourceContents.length)
                  return null

                return (
                  <Dropdown overlay={(
                    LanguageTypes.map(language => {
                      const languageContents = sourceContents.filter(c => c.language === language)

                      if (!languageContents.length)
                        return null

                      const displayContent = content

                      return (
                        <DropdownGroup name={LanguageNames[language]}>
                          {languageContents.map(content => (
                            <Disableable disabled={content === displayContent}>
                              <BookReaderLink id={book.id} contentId={content.id}>
                                <DropdownItem>
                                  <NewTabLink href={content.sourceUrl}>
                                    <LinkOutlined className='pr-2 text-blue-600' />
                                  </NewTabLink>

                                  {content.sourceUrl}
                                </DropdownItem>
                              </BookReaderLink>
                            </Disableable>
                          ))}
                        </DropdownGroup>
                      )
                    })
                  )}>

                    <SourceButton type={type} />
                  </Dropdown>
                )
              })}
            </div>
          </div>
        </div>

        <div className='text-sm text-gray-500'>
          <div><ReadOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.pageCount' values={{ count: content.pageCount }} /></div>
          <div><UploadOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.uploadTime' values={{ time: <TimeDisplay value={book.createdTime} /> }} /></div>
          <div><HistoryOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.updateTime' values={{ time: <TimeDisplay value={book.updatedTime} /> }} /></div>
        </div>
      </div>
    </div>
  )
}

const Tag = ({ type: tag, value }: { type: BookTag, value: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    borderColor: convertHex(BookTagColors[tag], hover ? 0.3 : 0.15),
    backgroundColor: convertHex(BookTagColors[tag], hover ? 0.2 : 0.1)
  })

  return (
    <animated.div
      style={{ ...style, color: BookTagColors[tag] }}
      className='inline-block px-1 mr-1 border rounded-sm overflow-hidden leading-normal cursor-pointer'
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      {value}
    </animated.div>
  )
}

const SourceButton = ({ type }: { type: ScraperType }) => {
  const [hover, setHover] = useState(false)
  const { info: { scrapers } } = useClientInfo()

  const style = useSpring({
    borderColor: convertHex(colors.gray[800], hover ? 0.3 : 0.15),
    backgroundColor: convertHex(colors.gray[800], hover ? 0.2 : 0.1)
  })

  return (
    <animated.div
      style={style}
      className='p-1 space-x-1 rounded overflow-hidden border cursor-pointer text-gray-500'
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      <img className='inline rounded-full h-6 w-auto' alt={type} src={`/assets/icons/${type}.jpg`} />

      <span className='text-sm'>{scrapers.find(s => s.type === type)?.name}</span>
    </animated.div>
  )
}
