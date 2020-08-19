import React, { useState } from 'react'
import { Tooltip } from '../../Components/Tooltip'
import { FormattedMessage } from 'react-intl'
import { ObjectType, Collection } from 'nhitomi-api'
import { PlusOutlined } from '@ant-design/icons'
import { RoundIconButton } from '../../Components/RoundIconButton'
import { useClient } from '../../ClientManager'
import { useProgress } from '../../ProgressManager'
import { useNotify } from '../../NotificationManager'
import { Disableable } from '../../Components/Disableable'
import { Prefetch } from '../../Prefetch'
import { useCollectionEditPrefetch } from '../Edit'
import { useLocalized } from '../../LocaleManager'

export const Menu = () => <>
  <NewButton />
</>

const NewButton = () => {
  const client = useClient()
  const { begin, end } = useProgress()
  const { notifyError } = useNotify()
  const [loading, setLoading] = useState(false)
  const [created, setCreated] = useState<Collection>()

  const dummyName = useLocalized('pages.collectionListing.book.menu.create.dummyName')

  return (
    <Tooltip placement='bottom' overlay={<FormattedMessage id='pages.collectionListing.book.menu.create.item' />}>
      <Disableable disabled={loading}>
        <RoundIconButton onClick={async () => {
          begin()
          setLoading(true)

          try {
            const created = await client.collection.createCollection({
              createCollectionRequest: {
                type: ObjectType.Book,
                collection: {
                  name: dummyName
                }
              }
            })

            setCreated(created)
          }
          catch (e) {
            notifyError(e)
            setLoading(false)
          }
          finally {
            end()
          }
        }}>

          <PlusOutlined />
        </RoundIconButton>
      </Disableable>

      {created && (
        <Prefetch fetch={useCollectionEditPrefetch} options={{ id: created.id }} />
      )}
    </Tooltip>
  )
}
