import React from 'react'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { Collection } from '../Client'

export function getCollectionContentPrefetch(id: string): Prefetch<Collection> {
  return {
    path: `/collections/${id}`,

    func: client => client.collection.getCollection({ id })
  }
}

export const CollectionContent = ({ id }: { id: string }) => {
  const { result } = usePrefetch(getCollectionContentPrefetch(id))

  if (result)
    return <Loaded result={result} />

  return null
}

export const CollectionContentLink = ({ id, ...props }: PrefetchLinkProps & { id: string }) => <PrefetchLink fetch={getCollectionContentPrefetch(id)} {...props} />

const Loaded = ({ result: { name } }: { result: Collection }) => {
  return null
}
