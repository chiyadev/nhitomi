import React, { Dispatch, useRef, useCallback, useState } from 'react'
import { BookPrefetchResult } from '.'
import { Container } from '../Components/Container'
import { useClient } from '../ClientManager'
import { useNotify } from '../NotificationManager'
import { useProgress } from '../ProgressManager'
import { useSpring, animated } from 'react-spring'
import { LoadContainer } from '../Components/LoadContainer'
import { DefaultQueryLimit } from '../BookListing/search'
import { Book, Collection } from 'nhitomi-api'
import { BookList, BookListItem, selectContent } from '../Components/BookList'
import { useConfig } from '../ConfigManager'
import { useTabTitle } from '../TitleSetter'
import { Dropdown } from '../Components/Dropdown'
import { usePrefetch } from '../Prefetch'
import { useCollectionListingPrefetch } from '../CollectionListing'
import { RoundIconButton } from '../Components/RoundIconButton'
import { DeleteOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { FlatButton } from '../Components/FlatButton'
import { Disableable } from '../Components/Disableable'
import { EmptyIndicator } from '../Components/EmptyIndicator'

export const BookDisplay = ({ result, setResult }: { result: BookPrefetchResult, setResult: Dispatch<BookPrefetchResult> }) => {
  const { collection, items } = result

  useTabTitle(collection.name)

  const [language] = useConfig('language')
  const [searchLanguages] = useConfig('searchLanguages')

  const contentLanguages = [language, ...searchLanguages]
  const contentSelector = useCallback((book: BookListItem) => selectContent(book.contents, contentLanguages), [contentLanguages.join(',')]) // eslint-disable-line

  return (
    <Container className='divide-y divide-gray-900'>
      <div className='p-2'>
        <div className='text-2xl'>{collection.name}</div>
        <div className='text-xs text-gray-800'>{collection.description}</div>
      </div>

      <div className='py-2'>
        <BookList
          items={items}
          contentSelector={contentSelector}
          menu={<>
            <DeleteButton collection={collection} />
          </>}
          empty={(
            <EmptyIndicator>
              <FormattedMessage id='pages.collectionContent.book.empty' />
            </EmptyIndicator>
          )} />

        <Loader result={result} setResult={setResult} />
      </div>
    </Container>
  )
}

const DeleteButton = ({ collection }: { collection: Collection }) => {
  const client = useClient()
  const [loading, setLoading] = useState(false)
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const [, navigateListing] = usePrefetch(useCollectionListingPrefetch, { id: collection.ownerIds[0] })

  return (
    <Dropdown
      visible={loading || undefined}
      placement='bottom'
      padding={false}
      overlayClassName='flex flex-col space-y-2 p-2'
      overlay={<>
        <div><FormattedMessage id='pages.collectionContent.book.delete.warning' /></div>

        <Disableable disabled={loading}>
          <FlatButton className='w-full' onClick={async () => {
            if (loading)
              return

            setLoading(true)
            begin()

            try {
              await client.collection.deleteCollection({ id: collection.id })
              await navigateListing()
            }
            catch (e) {
              notifyError(e)
            }
            finally {
              setLoading(false)
              end()
            }
          }}>

            <FormattedMessage id='pages.collectionContent.book.delete.button' />
          </FlatButton>
        </Disableable>
      </>}>

      <RoundIconButton >
        <DeleteOutlined />
      </RoundIconButton>
    </Dropdown>
  )
}

const Loader = ({ result, setResult }: { result: BookPrefetchResult, setResult: Dispatch<BookPrefetchResult> }) => {
  const { collection } = result

  const client = useClient()
  const { notifyError } = useNotify()
  const { begin, end: endProgress } = useProgress()

  const loadId = useRef(result.nextOffset >= collection.items.length ? -1 : 0)

  const style = useSpring({
    opacity: loadId.current < 0 ? 0 : 1
  })

  return (
    <animated.div style={style}>
      <LoadContainer
        key={loadId.current} // recreate load container for each load
        className='w-full h-20'
        onLoad={async () => {
          if (loadId.current < 0)
            return

          begin()

          try {
            const ids = collection.items.slice(result.nextOffset, result.nextOffset + DefaultQueryLimit)
            const moreResult = ids.length ? await client.book.getBooks({ getBookManyRequest: { ids } }) : []

            if (loadId.current < 0)
              return

            if (!moreResult.length) {
              loadId.current = -1
              return
            }

            // remove duplicates
            const items: Book[] = []
            const exists: { [id: string]: true } = {}

            for (const item of [...result.items, ...moreResult]) {
              if (!exists[item.id])
                items.push(item)

              exists[item.id] = true
            }

            setResult({
              ...result,

              items,
              nextOffset: result.nextOffset + DefaultQueryLimit
            })

            ++loadId.current
          }
          catch (e) {
            notifyError(e)
          }
          finally {
            endProgress()
          }
        }} />
    </animated.div>
  )
}
