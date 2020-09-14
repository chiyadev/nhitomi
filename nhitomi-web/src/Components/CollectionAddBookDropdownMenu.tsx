import React from 'react'
import { useClient, useClientInfo } from '../ClientManager'
import { useNotify, useAlert } from '../NotificationManager'
import { useState } from 'react'
import { Collection, CollectionInsertPosition } from 'nhitomi-api'
import { useAsync } from 'react-use'
import { Loading3QuartersOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { DropdownItem } from './Dropdown'
import { Disableable } from './Disableable'
import { useProgress } from '../ProgressManager'
import { CollectionContentLink } from '../CollectionContent'
import { usePageState } from '../state'

type BasicBook = {
  id: string
}

export const CollectionAddBookDropdownMenu = ({ book }: { book: BasicBook }) => {
  const client = useClient()
  const { info } = useClientInfo()
  const { notifyError } = useNotify()
  const [collections, setCollections] = usePageState<Collection[]>('userCollections')

  useAsync(async () => {
    if (collections)
      return

    try {
      if (!info.authenticated)
        throw Error('Unauthenticated.')

      const { items } = await client.user.getUserCollections({ id: info.user.id })

      setCollections(items)
    }
    catch (e) {
      notifyError(e)
      setCollections(undefined)
    }
  }, [])

  return <>
    {collections
      ? collections.map(collection => (
        <CollectionAddItem book={book} collection={collection} />
      ))
      : (
        <Disableable disabled>
          <DropdownItem>
            <Loading3QuartersOutlined className='animate-spin' />
          </DropdownItem>
        </Disableable>
      )}
  </>
}

const CollectionAddItem = ({ book, collection }: { book: BasicBook, collection: Collection }) => {
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
