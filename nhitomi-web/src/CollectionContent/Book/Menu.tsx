import React, { useState } from 'react'
import { useClient } from '../../ClientManager'
import { useNotify } from '../../NotificationManager'
import { useProgress } from '../../ProgressManager'
import { Collection } from 'nhitomi-api'
import { Dropdown } from '../../Components/Dropdown'
import { usePrefetch } from '../../Prefetch'
import { useCollectionListingPrefetch } from '../../CollectionListing'
import { RoundIconButton } from '../../Components/RoundIconButton'
import { DeleteOutlined, EditOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { FlatButton } from '../../Components/FlatButton'
import { Disableable } from '../../Components/Disableable'
import { CollectionEditLink } from '../../CollectionListing/Edit'
import { Tooltip } from '../../Components/Tooltip'

export const Menu = ({ collection }: { collection: Collection }) => <>
  <EditButton collection={collection} />
  <DeleteButton collection={collection} />
</>

const EditButton = ({ collection }: { collection: Collection }) => (
  <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionContent.book.menu.edit' />}>
    <CollectionEditLink id={collection.id}>
      <RoundIconButton >
        <EditOutlined />
      </RoundIconButton>
    </CollectionEditLink>
  </Tooltip>
)

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
        <div><FormattedMessage id='pages.collectionContent.book.menu.delete.warning' /></div>

        <Disableable disabled={loading}>
          <FlatButton
            icon={<DeleteOutlined />}
            className='w-full text-red'
            onClick={async () => {
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

            <FormattedMessage id='pages.collectionContent.book.menu.delete.button' />
          </FlatButton>
        </Disableable>
      </>}>

      <RoundIconButton >
        <DeleteOutlined />
      </RoundIconButton>
    </Dropdown>
  )
}
