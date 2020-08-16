import React, { useState, useCallback } from 'react'
import { Collection, ObjectType, User } from 'nhitomi-api'
import { PrefetchGenerator, usePostfetch, TypedPrefetchLinkProps, PrefetchLink, BackLink, usePrefetch } from '../../Prefetch'
import { useClient, useClientInfo } from '../../ClientManager'
import { PageContainer } from '../../Components/PageContainer'
import { useQueryState, usePageState } from '../../state'
import { Container } from '../../Components/Container'
import { FormattedMessage } from 'react-intl'
import { Input } from '../../Components/Input'
import { FilledButton } from '../../Components/FilledButton'
import { LeftOutlined, Loading3QuartersOutlined } from '@ant-design/icons'
import { FlatButton } from '../../Components/FlatButton'
import { Disableable } from '../../Components/Disableable'
import { useNotify } from '../../NotificationManager'
import { useCollectionListingPrefetch } from '..'
import { useTabTitle } from '../../TitleSetter'
import { useLocalized } from '../../LocaleManager'

export type PrefetchResult = { type: ObjectType, user: User, collections: Collection[] }
export type PrefetchOptions = { type?: ObjectType }

export const useCollectionCreatePrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ mode, type: targetType }) => {
  const client = useClient()
  const { info } = useClientInfo()
  const [currentType] = useQueryState<ObjectType>('replace', 'type')

  const type = targetType || (mode === 'postfetch' && currentType) || ObjectType.Book

  return {
    destination: {
      path: '/collections/new',
      query: { type }
    },

    fetch: async () => {
      if (!info.authenticated)
        throw Error('Unauthorized.')

      const { items } = await client.user.getUserCollections({ id: info.user.id })

      return {
        type,
        user: info.user,
        collections: items.filter(item => item.type === type)
      }
    }
  }
}

export const CollectionCreateLink = (props: TypedPrefetchLinkProps) => (
  <PrefetchLink fetch={useCollectionCreatePrefetch} options={{}} {...props} />
)

export const CollectionCreate = () => {
  const { result } = usePostfetch(useCollectionCreatePrefetch, {})

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  )
}

const Loaded = ({ type, user, collections }: PrefetchResult) => {
  useTabTitle(useLocalized('pages.collectionListing.create.title'))

  const [loading, setLoading] = useState(false)

  const client = useClient()
  const { notifyError } = useNotify()
  const [, navigateListing] = usePrefetch(useCollectionListingPrefetch, { id: user.id })

  const [name, setName] = usePageState('name', '')
  const [description, setDescription] = usePageState('description', '')

  const submit = useCallback(async () => {
    if (loading)
      return

    setLoading(true)

    try {
      await client.collection.createCollection({
        createCollectionRequest: {
          type,
          collection: {
            name,
            description
          }
        }
      })

      await navigateListing()
    }
    catch (e) {
      notifyError(e)
    }
    finally {
      setLoading(false)
    }
  }, [client.collection, description, loading, name, navigateListing, notifyError, type])

  return (
    <Container className='divide-y divide-gray-900'>
      <div className='p-2'>
        <div className='text-2xl'><FormattedMessage id='pages.collectionListing.create.title' /></div>
        <div className='text-xs text-gray-800'><FormattedMessage id='pages.collectionListing.create.subtitle' values={{ type: <FormattedMessage id={`types.objectType.${type}`} /> }} /></div>
      </div>

      <Disableable disabled={loading}>
        <div className='p-2 space-y-4 text-sm'>
          <div>
            <div className='mb-1'><FormattedMessage id='pages.collectionListing.create.name' /></div>

            <Input
              className='w-full max-w-sm'
              autoFocus
              allowClear
              value={name}
              setValue={setName}
              status={(
                collections.findIndex(c => c.name.toLowerCase() === name.toLowerCase()) !== -1
                  ? { status: 'warning', help: <FormattedMessage id='pages.collectionListing.create.nameTaken' values={{ name }} /> }
                  : undefined
              )}
              onSubmit={submit} />
          </div>

          <div>
            <div className='mb-1'><FormattedMessage id='pages.collectionListing.create.description' /></div>

            <Input
              type='textarea'
              className='w-full max-w-sm'
              value={description}
              setValue={setDescription}
              onSubmit={submit} />
          </div>

          <div className='space-x-1'>
            <BackLink>
              <FlatButton className='inline-block' icon={<LeftOutlined />}>
                <FormattedMessage id='pages.collectionListing.create.cancel' />
              </FlatButton>
            </BackLink>

            <FilledButton className='inline-block' type='primary' onClick={submit} icon={loading && <Loading3QuartersOutlined className='animate-spin' />}>
              <FormattedMessage id='pages.collectionListing.create.submit' />
            </FilledButton>
          </div>
        </div>
      </Disableable>
    </Container>
  )
}
