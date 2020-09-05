import React, { useState, useMemo } from 'react'
import { BookTag, ScraperType } from 'nhitomi-api'
import { useClient, useClientInfo } from '../ClientManager'
import { CoverImage } from '../Components/CoverImage'
import { cx } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { useLayout } from '../LayoutManager'
import { UploadOutlined, HistoryOutlined, LinkOutlined, ReadOutlined, ReloadOutlined, WarningOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { TimeDisplay } from '../Components/TimeDisplay'
import { BookTagColors } from '../Components/colors'
import { getColor } from '../theme'
import { Dropdown, DropdownItem, DropdownGroup } from '../Components/Dropdown'
import { PrefetchResult, BookReaderLink } from '.'
import { BookTags, ScraperTypes, LanguageTypes } from '../orderedConstants'
import { LanguageNames } from '../LocaleManager'
import { NewTabLink } from '../Components/NewTabLink'
import { BookListingLink } from '../BookListing'
import { Disableable } from '../Components/Disableable'
import { useConfig } from '../ConfigManager'
import { FlatButton } from '../Components/FlatButton'
import { useContentSelector } from '../Components/BookList'
import { useAsync } from 'react-use'
import { useNotify } from '../NotificationManager'

export const Info = ({ book, content }: PrefetchResult) => {
  const client = useClient()
  const { screen } = useLayout()
  const selectContent = useContentSelector()

  const [preferEnglishName] = useConfig('bookReaderPreferEnglishName')

  return (
    <div className={cx('flex p-4', screen === 'sm' ? 'flex-col space-y-4' : 'flex-row space-x-4')}>
      {useMemo(() => (
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
      ), [book.id, client.book, content.id, screen])}

      <div className='flex-1 space-y-4'>
        {useMemo(() => (
          <div>
            <BookListingLink query={{ query: (preferEnglishName && book.englishName) || book.primaryName }}>
              <div className='text-2xl font-bold'>{(preferEnglishName && book.englishName) || book.primaryName}</div>
            </BookListingLink>

            <BookListingLink query={{ query: (!preferEnglishName && book.englishName) || book.primaryName }}>
              <div className='text-sm text-gray-darker'>{(!preferEnglishName && book.englishName) || book.primaryName}</div>
            </BookListingLink>
          </div>
        ), [book.englishName, book.primaryName, preferEnglishName])}

        {useMemo(() => BookTags.map(tag => {
          const tags = book.tags[tag]

          if (!tags)
            return null

          return (
            <div>
              <div className='text-xs text-gray-darker mb-1'><FormattedMessage id={`types.bookTag.${tag}`} /></div>
              <div className='text-sm leading-tight'>
                {tags.sort().map(value => (
                  <BookListingLink query={{ query: `${tag}:${value.replace(/\s/g, '_')}` }}>
                    <Tag type={tag} value={value} />
                  </BookListingLink>
                ))}
              </div>
            </div>
          )
        }), [book.tags])}

        {useMemo(() => (
          <div>
            <div className='text-xs text-gray-darker mb-1'><FormattedMessage id='pages.bookReader.sources' /></div>
            <div className='space-x-1'>
              {ScraperTypes.map(type => {
                const sourceContents = book.contents.filter(c => c.source === type).sort((a, b) => b.id.localeCompare(a.id))
                const linkContent = content.source === type ? content : selectContent(sourceContents)

                if (!linkContent)
                  return null

                return (
                  <Dropdown className='inline-flex' overlay={(
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
                                    <LinkOutlined className='pr-2 text-blue' />
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

                    <NewTabLink href={linkContent.sourceUrl}>
                      <SourceButton type={type} />
                    </NewTabLink>
                  </Dropdown>
                )
              })}
            </div>
          </div>
        ), [book.contents, book.id, content, selectContent])}

        {useMemo(() => (
          <div className='text-xs text-gray'>
            <div><ReadOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.pageCount' values={{ count: content.pageCount }} /></div>
            <div><UploadOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.uploadTime' values={{ time: <TimeDisplay value={book.createdTime} /> }} /></div>
            <div><HistoryOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.updateTime' values={{ time: <TimeDisplay value={book.updatedTime} /> }} /></div>
            <div><RefreshStatus book={book} content={content} /></div>
          </div>
        ), [book, content])}
      </div>
    </div>
  )
}

const Tag = ({ type: tag, value }: { type: BookTag, value: string }) => {
  const [hover, setHover] = useState(false)

  const style = useSpring({
    color: getColor(BookTagColors[tag], 'lighter').hex,
    borderColor: getColor(BookTagColors[tag], 'darker').opacity(0.125).tint(hover ? 0.25 : 0).rgb,
    backgroundColor: getColor(BookTagColors[tag], 'darker').opacity(0.125).tint(hover ? 0.25 : 0).rgb
  })

  return (
    <animated.div
      style={style} // inline-block for spacing between lines
      className='inline-block px-1 mr-1 border rounded-sm overflow-hidden leading-normal cursor-pointer'
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}>

      {value}
    </animated.div>
  )
}

const SourceButton = ({ type }: { type: ScraperType }) => {
  const { info: { scrapers } } = useClientInfo()

  return (
    <FlatButton icon={(
      <img className='rounded-full h-6 w-auto align-middle' alt={type} src={`/assets/icons/${type}.jpg`} />
    )}>
      <span className='text-sm text-gray'>{scrapers.find(s => s.type === type)?.name}</span>
    </FlatButton>
  )
}

const RefreshStatus = ({ book, content }: PrefetchResult) => {
  const client = useClient()
  const { notifyError } = useNotify()

  const { loading, value, error } = useAsync(async () => {
    if (content.refreshTime && Date.now() - content.refreshTime.getTime() < 1000 * 60 * 60 * 24)
      return content.isAvailable

    const refreshed = await client.book.refreshBook({
      id: book.id,
      refreshContentRequest: {
        contentId: content.id
      }
    })

    return refreshed.contents.find(c => c.id === content.id)?.isAvailable
  }, [])

  return loading
    ? (
      <div><ReloadOutlined className='w-4 text-center animate-spin' /> <FormattedMessage id='pages.bookReader.available.refresh' /></div>
    ) : error
      ? (
        <div className='text-red cursor-pointer' onClick={() => notifyError(error)}><WarningOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.available.error' /></div>
      )
      : value ? null : (
        <div className='text-red'><WarningOutlined className='w-4 text-center' /> <FormattedMessage id='pages.bookReader.available.false' /></div>
      )
}
