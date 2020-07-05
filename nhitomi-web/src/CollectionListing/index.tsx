import React from 'react'
import { CollectionSearchResult, User } from '../Client'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { useScrollShortcut } from '../shortcuts'
import { useTabTitleFormatted } from '../hooks'

type Fetched = {
  user: User
  collections: CollectionSearchResult
}

export function getCollectionListingPrefetch(id?: string): Prefetch<Fetched> {
  return {
    path: '/collections',

    func: async client => {
      if (id) {
        const [user, collections] = await Promise.all([client.user.getUser({ id }), client.user.getUserCollections({ id })])

        return { user, collections }
      }

      if (client.currentInfo.authenticated) {
        const [user, collections] = [client.currentInfo.user, await client.user.getUserCollections(client.currentInfo.user)]

        return { user, collections }
      }

      throw Error('Unauthenticated')
    }
  }
}

export const CollectionListing = ({ id }: { id?: string }) => {
  const { result } = usePrefetch(getCollectionListingPrefetch(id))

  if (result)
    return <Loaded result={result} />

  return null
}

export const CollectionListingLink = (props: PrefetchLinkProps) => <PrefetchLink fetch={getCollectionListingPrefetch()} {...props} />

const Loaded = ({ result: { user, collections: { items } } }: { result: Fetched }) => {
  useScrollShortcut()
  useTabTitleFormatted('collectionListing.header.title')

  return null
}
