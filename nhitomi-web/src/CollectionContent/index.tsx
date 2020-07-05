import React from 'react'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { Collection, ObjectType } from '../Client'
import { CollectionContentBookView, BookCollectionManager, BookCollectionLoadResult } from './BookView'

export type Fetched = {
  type: ObjectType.Book
  collection: Collection
  result: BookCollectionLoadResult
}

export function getCollectionContentPrefetch(id: string): Prefetch<Fetched> {
  return {
    path: `/collections/${id}`,

    func: async client => {
      const collection = await client.collection.getCollection({ id })

      switch (collection.type) {
        case ObjectType.Book:
          return {
            type: collection.type,
            collection,
            result: await new BookCollectionManager(client, collection).refresh()
          }
      }

      throw Error(`Unsupported collection type '${collection.type}'.`)
    }
  }
}

export const CollectionContent = ({ id }: { id: string }) => {
  const { result, dispatch } = usePrefetch(getCollectionContentPrefetch(id))

  switch (result?.type) {
    case ObjectType.Book:
      return <CollectionContentBookView fetched={result} dispatch={dispatch} />
  }

  return null
}

export const CollectionContentLink = ({ id, ...props }: PrefetchLinkProps & { id: string }) => <PrefetchLink fetch={getCollectionContentPrefetch(id)} {...props} />
