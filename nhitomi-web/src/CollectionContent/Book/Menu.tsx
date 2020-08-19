import React, { useState } from 'react'
import { useClient, useClientInfo } from '../../ClientManager'
import { useNotify } from '../../NotificationManager'
import { useProgress } from '../../ProgressManager'
import { Collection, SpecialCollection, CollectionInsertPosition } from 'nhitomi-api'
import { Dropdown, DropdownItem } from '../../Components/Dropdown'
import { usePrefetch, useDynamicPrefetch } from '../../Prefetch'
import { useCollectionListingPrefetch } from '../../CollectionListing'
import { RoundIconButton } from '../../Components/RoundIconButton'
import { DeleteOutlined, EditOutlined, HeartOutlined, EyeOutlined, StarOutlined, CopyOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { FlatButton } from '../../Components/FlatButton'
import { Disableable } from '../../Components/Disableable'
import { CollectionEditLink } from '../../CollectionListing/Edit'
import { Tooltip } from '../../Components/Tooltip'
import { cx } from 'emotion'
import { useCollectionContentPrefetch } from '..'

export const Menu = ({ collection }: { collection: Collection }) => <>
  <SpecialButton collection={collection} />
  <EditButton collection={collection} />
  <DuplicateButton collection={collection} />
  <DeleteButton collection={collection} />
</>

const SpecialButton = ({ collection }: { collection: Collection }) => {
  const client = useClient()
  const { info, setInfo, fetchInfo } = useClientInfo()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)

  if (!info.authenticated)
    return null

  const setSpecial = async (special: SpecialCollection) => {
    begin()
    setLoading(true)

    try {
      const info = await fetchInfo()

      if (!info.authenticated)
        throw Error('Unauthenticated.')

      const user = await client.user.updateUser({
        id: info.user.id,
        userBase: {
          ...info.user,
          specialCollections: {
            ...info.user.specialCollections,
            book: {
              ...info.user.specialCollections?.book,
              [special]: collection.id
            }
          }
        }
      })

      setInfo({ ...info, user })
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      end()
      setLoading(false)
    }
  }

  return (
    <Dropdown
      placement='bottom'
      overlay={(
        <Disableable disabled={loading}>
          <DropdownItem icon={<HeartOutlined className={cx({ 'text-red': info.user.specialCollections?.book?.favorites === collection.id })} />} onClick={() => setSpecial(SpecialCollection.Favorites)}>
            <FormattedMessage id='pages.collectionContent.book.menu.special.favorites' />
          </DropdownItem>

          <DropdownItem icon={<EyeOutlined className={cx({ 'text-blue': info.user.specialCollections?.book?.later === collection.id })} />} onClick={() => setSpecial(SpecialCollection.Later)}>
            <FormattedMessage id='pages.collectionContent.book.menu.special.later' />
          </DropdownItem>
        </Disableable>
      )}>

      <RoundIconButton>
        {info.user.specialCollections?.book?.favorites === collection.id ? <HeartOutlined className='text-red' />
          : info.user.specialCollections?.book?.later === collection.id ? <EyeOutlined className='text-blue' />
            : <StarOutlined />}
      </RoundIconButton>
    </Dropdown>
  )
}

const EditButton = ({ collection }: { collection: Collection }) => (
  <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionContent.book.menu.edit' />}>
    <CollectionEditLink id={collection.id}>
      <RoundIconButton>
        <EditOutlined />
      </RoundIconButton>
    </CollectionEditLink>
  </Tooltip>
)

const DuplicateButton = ({ collection }: { collection: Collection }) => {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)
  const [prefetchNode, navigate] = useDynamicPrefetch(useCollectionContentPrefetch)

  return (
    <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionContent.book.menu.duplicate' />}>
      <Disableable disabled={loading}>
        <RoundIconButton onClick={async () => {
          begin()
          setLoading(true)

          try {
            let created = await client.collection.createCollection({
              createCollectionRequest: {
                type: collection.type,
                collection
              }
            })

            created = await client.collection.addCollectionItems({
              id: created.id,
              addCollectionItemsRequest: {
                items: collection.items,
                position: CollectionInsertPosition.Start
              }
            })

            await navigate({ id: created.id })
          }
          catch (e) {
            notifyError(e)
          }
          finally {
            end()
            setLoading(false)
          }
        }}>

          <CopyOutlined />
        </RoundIconButton>
      </Disableable>

      {prefetchNode}
    </Tooltip>
  )
}

const DeleteButton = ({ collection }: { collection: Collection }) => {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)
  const [, navigateListing] = usePrefetch(useCollectionListingPrefetch, { id: collection.ownerIds[0] })

  return (
    <Dropdown
      placement='bottom'
      padding={false}
      overlayClassName='flex flex-col space-y-2 p-2'
      overlay={<>
        <div><FormattedMessage id='pages.collectionContent.book.menu.delete.warning' /></div>

        <Disableable disabled={loading}>
          <FlatButton
            icon={<DeleteOutlined />}
            className='w-full text-red'
            onClick={async () => {
              begin()
              setLoading(true)

              try {
                await client.collection.deleteCollection({ id: collection.id })
                await navigateListing()
              }
              catch (e) {
                notifyError(e)
              }
              finally {
                end()
                setLoading(false)
              }
            }}>

            <FormattedMessage id='pages.collectionContent.book.menu.delete.button' />
          </FlatButton>
        </Disableable>
      </>}>

      <RoundIconButton>
        <DeleteOutlined />
      </RoundIconButton>
    </Dropdown>
  )
}
