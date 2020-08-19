import React, { useState, useCallback } from 'react'
import { Collection, User } from 'nhitomi-api'
import { PrefetchGenerator, usePostfetch, TypedPrefetchLinkProps, PrefetchLink, BackLink, usePrefetch } from '../Prefetch'
import { useClient } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { usePageState } from '../state'
import { Container } from '../Components/Container'
import { FormattedMessage } from 'react-intl'
import { Input } from '../Components/Input'
import { FilledButton } from '../Components/FilledButton'
import { LeftOutlined, Loading3QuartersOutlined } from '@ant-design/icons'
import { FlatButton } from '../Components/FlatButton'
import { Disableable } from '../Components/Disableable'
import { useNotify } from '../NotificationManager'
import { useTabTitle } from '../TitleSetter'
import { useLocalized } from '../LocaleManager'
import { getColor } from '../theme'
import { useCollectionContentPrefetch } from '../CollectionContent'

export type PrefetchResult = { collection: Collection, owner: User }
export type PrefetchOptions = { id: string }

export const useCollectionEditPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id }) => {
  const client = useClient()

  return {
    destination: {
      path: `/collections/${id}/edit`
    },

    fetch: async () => {
      const collection = await client.collection.getCollection({ id })
      const owner = await client.user.getUser({ id: collection.ownerIds[0] })

      return { collection, owner }
    }
  }
}

export const CollectionEditLink = ({ id, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useCollectionEditPrefetch} options={{ id }} {...props} />
)

export const CollectionEdit = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useCollectionEditPrefetch, { requireAuth: true, ...options })

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  )
}

const Loaded = ({ collection, owner }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.collectionListing.edit.title'))

  const [loading, setLoading] = useState(false)

  const client = useClient()
  const { notifyError } = useNotify()
  const [, navigateCollection] = usePrefetch(useCollectionContentPrefetch, { id: collection.id })

  const [name, setName] = usePageState('name', collection.name)
  const [description, setDescription] = usePageState('description', collection.description)

  const submit = useCallback(async () => {
    if (loading)
      return

    setLoading(true)

    try {
      await client.collection.updateCollection({
        id: collection.id,
        collectionBase: {
          name,
          description
        }
      })

      await navigateCollection()
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      setLoading(false)
    }
  }, [client.collection, collection.id, description, loading, name, navigateCollection, notifyError])

  return (
    <Container className='divide-y divide-gray-darkest'>
      <div className='p-2'>
        <div className='text-2xl'><FormattedMessage id='pages.collectionListing.edit.title' /></div>
        <div className='text-xs text-gray-darker'><FormattedMessage id='pages.collectionListing.edit.subtitle' values={{ collection: collection.name, owner: owner.username }} /></div>
      </div>

      <Disableable disabled={loading}>
        <div className='p-2 space-y-4 text-sm'>
          <div>
            <div className='mb-1'><FormattedMessage id='pages.collectionListing.edit.name' /></div>

            <Input className='w-full max-w-sm' autoFocus allowClear value={name} setValue={setName} onSubmit={submit} />
          </div>

          <div>
            <div className='mb-1'><FormattedMessage id='pages.collectionListing.edit.description' /></div>

            <Input
              type='textarea'
              className='w-full max-w-sm'
              value={description}
              setValue={setDescription}
              onSubmit={submit} />
          </div>

          <div className='space-x-1'>
            <BackLink>
              <FlatButton icon={<LeftOutlined />}>
                <FormattedMessage id='pages.collectionListing.edit.cancel' />
              </FlatButton>
            </BackLink>

            <FilledButton color={getColor('blue')} onClick={submit} icon={loading && <Loading3QuartersOutlined className='animate-spin' />}>
              <FormattedMessage id='pages.collectionListing.edit.submit' />
            </FilledButton>
          </div>
        </div>
      </Disableable>
    </Container>
  )
}
