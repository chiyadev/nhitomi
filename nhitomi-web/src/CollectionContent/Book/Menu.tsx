import React, { useState } from 'react'
import { useClient, useClientInfo } from '../../ClientManager'
import { useNotify } from '../../NotificationManager'
import { useProgress } from '../../ProgressManager'
import { Collection, SpecialCollection } from 'nhitomi-api'
import { Dropdown, DropdownItem } from '../../Components/Dropdown'
import { usePrefetch } from '../../Prefetch'
import { useCollectionListingPrefetch } from '../../CollectionListing'
import { RoundIconButton } from '../../Components/RoundIconButton'
import { DeleteOutlined, EditOutlined, HeartOutlined, EyeOutlined, StarOutlined, InfoCircleOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { FlatButton } from '../../Components/FlatButton'
import { Disableable } from '../../Components/Disableable'
import { CollectionEditLink } from '../../CollectionListing/Edit'
import { Tooltip } from '../../Components/Tooltip'
import { cx } from 'emotion'
import { Anchor } from '../../Components/Anchor'

export const Menu = ({ collection }: { collection: Collection }) => <>
  <SpecialButton collection={collection} />
  <EditButton collection={collection} />
  <DeleteButton collection={collection} />
  <HelpButton />
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

const HelpButton = () => (
  <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionContent.book.menu.help' />}>
    <Anchor target='_blank' href='https://github.com/chiyadev/nhitomi/wiki/Managing-collections-on-the-website'>
      <RoundIconButton>
        <InfoCircleOutlined />
      </RoundIconButton>
    </Anchor>
  </Tooltip>
)
