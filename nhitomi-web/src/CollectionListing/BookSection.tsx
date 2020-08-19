import React, { useCallback, useMemo } from 'react'
import { BookCollection } from '.'
import { BookList, selectContent, BookListItem } from '../Components/BookList'
import { useConfig } from '../ConfigManager'
import { BookContent, ObjectType } from 'nhitomi-api'
import { CollectionContentLink } from '../CollectionContent'
import { Tooltip } from '../Components/Tooltip'
import { CollectionCreateLink } from './Create'
import { RoundIconButton } from '../Components/RoundIconButton'
import { PlusOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { EmptyIndicator } from '../Components/EmptyIndicator'

// instead of reimplementing a new list for book collections, adapt BookList for code reuse
export const BookSection = ({ collections }: { collections: BookCollection[] }) => {
  const [language] = useConfig('language')
  const [searchLanguages] = useConfig('searchLanguages')

  const contentLanguages = [language, ...searchLanguages]
  const contentSelector = useCallback((book: BookListItem) => selectContent(book.contents, contentLanguages), [contentLanguages.join(',')]) // eslint-disable-line

  const items = useMemo(() => collections.map(({ collection, cover }) => ({
    ...cover || { contents: [] },

    id: collection.id, // use collection id instead of cover id
    primaryName: collection.name,
    englishName: collection.description
  })), [collections])

  const getCoverRequest = useCallback((book: BookListItem, content: BookContent) => ({
    id: collections.find(c => c.collection.id === book.id)?.cover?.id!, // convert collection id to cover id
    contentId: content.id,
    index: -1
  }), [collections])

  return (
    <BookList
      items={items}
      contentSelector={contentSelector}
      preferEnglishName={false} // preferEnglishName would swap collection name and description
      overlayVisible
      getCoverRequest={getCoverRequest}
      LinkComponent={CollectionContentLink}
      menu={<>
        <NewButton />
      </>}
      empty={(
        <EmptyIndicator>
          <FormattedMessage id='pages.collectionListing.empty' />
        </EmptyIndicator>
      )} />
  )
}

const NewButton = () => (
  <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionListing.create.title' />}>
    <CollectionCreateLink type={ObjectType.Book}>
      <RoundIconButton>
        <PlusOutlined />
      </RoundIconButton>
    </CollectionCreateLink>
  </Tooltip>
)
