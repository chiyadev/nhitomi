import React, { ReactNode, useState } from 'react'
import { DropdownItem, DropdownSubMenu, DropdownDivider } from '../Dropdown'
import { BookListItem, useBookList } from '.'
import { BookContent, BookTag, SpecialCollection, ObjectType, CollectionInsertPosition, Collection } from 'nhitomi-api'
import { BookReaderLink } from '../../BookReader'
import { FormattedMessage } from 'react-intl'
import { ExpandAltOutlined, SearchOutlined, LinkOutlined, HeartOutlined, EyeOutlined, PlusOutlined, Loading3QuartersOutlined } from '@ant-design/icons'
import { useConfig } from '../../ConfigManager'
import { BookListingLink } from '../../BookListing'
import { useAlert, useNotify } from '../../NotificationManager'
import { useCopyToClipboard, useAsync } from 'react-use'
import { useClient, useClientInfo } from '../../ClientManager'
import { useProgress } from '../../ProgressManager'
import { CollectionContentLink } from '../../CollectionContent'
import { Disableable } from '../Disableable'
import { Anchor } from '../Anchor'

export const Overlay = ({ book, content }: { book: BookListItem, content?: BookContent }) => {
  const { OverlayComponent } = useBookList()

  let rendered = <>
    {content && <>
      <OpenInNewTabItem book={book} content={content} />
    </>}

    <SearchItem book={book} />

    <DropdownDivider />

    <CopyToClipboardItem value={book.id} displayValue={<code>{book.id}</code>}>
      <FormattedMessage id='components.bookList.overlay.copy.id' />
    </CopyToClipboardItem>

    {content && <>
      <CopyToClipboardItem value={content.sourceUrl} displayValue={<Anchor target='_blank' className='text-blue' href={content.sourceUrl}>{content.sourceUrl}</Anchor>}>
        <FormattedMessage id='components.bookList.overlay.copy.source' />
      </CopyToClipboardItem>
    </>}

    <DropdownDivider />

    <CollectionQuickAddItem book={book} type={SpecialCollection.Favorites} />
    <CollectionQuickAddItem book={book} type={SpecialCollection.Later} />
    <CollectionAddItem book={book} />
  </>

  if (OverlayComponent) {
    rendered = (
      <OverlayComponent book={book} content={content} children={rendered} />
    )
  }

  return rendered
}

const OpenInNewTabItem = ({ book, content }: { book: BookListItem, content: BookContent }) => (
  <BookReaderLink id={book.id} contentId={content.id} target='_blank' rel='noopener noreferrer'>
    <DropdownItem icon={<ExpandAltOutlined />}>
      <FormattedMessage id='components.bookList.overlay.openNewTab' />
    </DropdownItem>
  </BookReaderLink>
)

const SearchItem = ({ book }: { book: BookListItem }) => {
  const [preferEnglishName] = useConfig('bookReaderPreferEnglishName')
  const name = (preferEnglishName && book.englishName) || book.primaryName

  return (
    <DropdownSubMenu
      name={<FormattedMessage id='components.bookList.overlay.searchBy.item' />}
      icon={<SearchOutlined />}>

      <BookListingLink query={{ query: name }}>
        <DropdownItem>
          <FormattedMessage id='components.bookList.overlay.searchBy.name' values={{ value: <span className='text-gray'>{name}</span> }} />
        </DropdownItem>
      </BookListingLink>

      <SearchItemPart type={BookTag.Artist} values={book.tags?.artist || []} />
      <SearchItemPart type={BookTag.Character} values={book.tags?.character || []} />
      <SearchItemPart type={BookTag.Tag} values={book.tags?.tag || []} />
    </DropdownSubMenu>
  )
}

const SearchItemPart = ({ type, values }: { type: BookTag, values: string[] }) => !values.length ? null : (
  <BookListingLink query={{ query: values.map(v => `${type}:${v.replace(/\s/g, '_')}`).join(' ') }}>
    <DropdownItem>
      <FormattedMessage id={`components.bookList.overlay.searchBy.${type}`} values={{ value: <span className='text-gray'>{values.join(', ')}</span> }} />
    </DropdownItem>
  </BookListingLink>
)

const CopyToClipboardItem = ({ children, value, displayValue }: { children?: ReactNode, value: string, displayValue?: ReactNode }) => {
  const { alert } = useAlert()
  const [, setClipboard] = useCopyToClipboard()

  return (
    <DropdownItem
      children={children}
      icon={<LinkOutlined />}
      onClick={() => {
        setClipboard(value)
        alert(<FormattedMessage id='components.bookList.overlay.copy.success' values={{ value: displayValue || value }} />, 'info')
      }} />
  )
}

const CollectionQuickAddItem = ({ book, type }: { book: BookListItem, type: SpecialCollection }) => {
  const client = useClient()
  const { info, setInfo } = useClientInfo()
  const { begin, end } = useProgress()
  const { alert } = useAlert()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)

  let icon: ReactNode

  switch (type) {
    case SpecialCollection.Favorites:
      icon = <HeartOutlined className='text-red' />
      break

    case SpecialCollection.Later:
      icon = <EyeOutlined className='text-blue' />
      break
  }

  return (
    <Disableable disabled={loading}>
      <DropdownItem
        icon={icon}
        onClick={async () => {
          begin()
          setLoading(true)

          try {
            if (!info.authenticated)
              throw Error('Unauthenticated.')

            let collection = info.user.specialCollections?.book?.[type]

            if (!collection) {
              collection = (await client.user.getUserSpecialCollection({ id: info.user.id, collection: type, type: ObjectType.Book })).id

              setInfo({
                ...info,
                user: {
                  ...info.user,
                  specialCollections: {
                    ...info.user.specialCollections,
                    book: {
                      ...info.user.specialCollections?.book,
                      [type]: collection
                    }
                  }
                }
              })
            }

            await client.collection.addCollectionItems({
              id: collection,
              addCollectionItemsRequest: {
                items: [book.id],
                position: CollectionInsertPosition.Start
              }
            })

            alert((
              <FormattedMessage
                id='components.bookList.overlay.collections.success'
                values={{
                  link: (
                    <CollectionContentLink id={collection} className='text-blue'>
                      <FormattedMessage id={`components.bookList.overlay.collections.${type}Add.collectionName`} />
                    </CollectionContentLink>
                  )
                }} />
            ), 'success')
          }
          catch (e) {
            notifyError(e)
          }
          finally {
            end()
            setLoading(false)
          }
        }}>

        <FormattedMessage id={`components.bookList.overlay.collections.${type}Add.item`} />
      </DropdownItem>
    </Disableable>
  )
}

const CollectionAddItem = ({ book }: { book: BookListItem }) => {
  const client = useClient()
  const { info } = useClientInfo()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)
  const [collections, setCollections] = useState<Collection[]>()

  useAsync(async () => {
    if (!loading || collections)
      return

    try {
      if (!info.authenticated)
        throw Error('Unauthenticated.')

      const { items } = await client.user.getUserCollections({ id: info.user.id })

      setCollections(items)
    }
    catch (e) {
      notifyError(e)
      setCollections([])
    }
    finally {
      setLoading(false)
    }
  }, [loading])

  return (
    <DropdownSubMenu
      name={<FormattedMessage id='components.bookList.overlay.collections.otherAdd' />}
      icon={<PlusOutlined />}
      onShow={() => { !collections && setLoading(true) }}>

      {loading && (
        <Disableable disabled>
          <DropdownItem>
            <Loading3QuartersOutlined className='animate-spin' />
          </DropdownItem>
        </Disableable>
      )}

      {collections?.map(collection => (
        <CollectionAddItemPart book={book} collection={collection} />
      ))}
    </DropdownSubMenu>
  )
}

const CollectionAddItemPart = ({ book, collection }: { book: BookListItem, collection: Collection }) => {
  const client = useClient()
  const { begin, end } = useProgress()
  const { alert } = useAlert()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)

  return (
    <Disableable disabled={loading}>
      <DropdownItem onClick={async () => {
        begin()
        setLoading(true)

        try {
          await client.collection.addCollectionItems({
            id: collection.id,
            addCollectionItemsRequest: {
              items: [book.id],
              position: CollectionInsertPosition.Start
            }
          })

          alert((
            <FormattedMessage
              id='components.bookList.overlay.collections.success'
              values={{
                link: (
                  <CollectionContentLink id={collection.id} className='text-blue'>{collection.name}</CollectionContentLink>
                )
              }} />
          ), 'success')
        }
        catch (e) {
          notifyError(e)
        }
        finally {
          end()
          setLoading(false)
        }
      }}>

        {collection.name}
      </DropdownItem>
    </Disableable>
  )
}
