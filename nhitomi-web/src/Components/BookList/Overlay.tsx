import React, { ReactNode, useState } from 'react'
import { DropdownItem, DropdownSubMenu, DropdownDivider } from '../Dropdown'
import { BookListItem, useBookList } from '.'
import { BookContent, BookTag, SpecialCollection, ObjectType, CollectionInsertPosition } from 'nhitomi-api'
import { BookReaderLink } from '../../BookReader'
import { FormattedMessage } from 'react-intl'
import { ExpandAltOutlined, SearchOutlined, LinkOutlined, HeartOutlined, EyeOutlined } from '@ant-design/icons'
import { useConfig } from '../../ConfigManager'
import { BookListingLink } from '../../BookListing'
import { useAlert, useNotify } from '../../NotificationManager'
import { useCopyToClipboard } from 'react-use'
import { NewTabLink } from '../NewTabLink'
import { useClient, useClientInfo } from '../../ClientManager'
import { useProgress } from '../../ProgressManager'
import { CollectionContentLink } from '../../CollectionContent'
import { Disableable } from '../Disableable'

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
      <CopyToClipboardItem value={content.sourceUrl} displayValue={<NewTabLink className='text-blue' href={content.sourceUrl}>{content.sourceUrl}</NewTabLink>}>
        <FormattedMessage id='components.bookList.overlay.copy.source' />
      </CopyToClipboardItem>
    </>}

    <DropdownDivider />

    <CollectionQuickAddItem book={book} type={SpecialCollection.Favorites} />
    <CollectionQuickAddItem book={book} type={SpecialCollection.Later} />
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
      icon = <HeartOutlined />
      break

    case SpecialCollection.Later:
      icon = <EyeOutlined />
      break
  }

  return (
    <Disableable disabled={loading}>
      <DropdownItem
        icon={icon}
        onClick={async () => {
          if (!info.authenticated)
            return

          begin()
          setLoading(true)

          try {
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
                id={`components.bookList.overlay.collections.${type}Add.success`}
                values={{
                  link: (
                    <CollectionContentLink id={collection} className='text-blue'>
                      <FormattedMessage id={`components.bookList.overlay.collections.${type}Add.successLink`} />
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
