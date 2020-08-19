import React, { Dispatch } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { Book, ObjectType, Collection } from 'nhitomi-api'
import { useClient } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'
import { DefaultQueryLimit } from '../BookListing/search'
import { BookDisplay } from './Book'
import { useScrollShortcut } from '../shortcut'

export type PrefetchResult =
  ({ type: 'book' } & BookPrefetchResult) |
  ({ type: 'other' })

export type BookPrefetchResult = {
  collection: Collection
  items: Book[]
  nextOffset: number
}

export type PrefetchOptions = { id: string }

export const useCollectionContentPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id }) => {
  const client = useClient()

  return {
    destination: {
      path: `/collections/${id}`
    },

    fetch: async () => {
      const collection = await client.collection.getCollection({ id })

      switch (collection.type) {
        case ObjectType.Book: {
          const ids = collection.items.slice(0, DefaultQueryLimit)

          return {
            type: 'book',
            collection,
            items: ids.length ? await client.book.getBooks({ getBookManyRequest: { ids } }) : [],
            nextOffset: DefaultQueryLimit
          }
        }

        default:
          return { type: 'other' }
      }
    }
  }
}

export const CollectionContentLink = ({ id, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useCollectionContentPrefetch} options={{ id }} {...props} />
)

export const CollectionContent = (options: PrefetchOptions) => {
  const { result, setResult } = usePostfetch(useCollectionContentPrefetch, { requireAuth: true, ...options })

  useScrollShortcut()

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded result={result} setResult={setResult} />
    </PageContainer>
  )
}

const Loaded = ({ result, setResult }: { result: PrefetchResult, setResult: Dispatch<PrefetchResult> }) => {
  switch (result.type) {
    case 'book':
      return (
        <BookDisplay
          result={result}
          setResult={result => setResult({ type: 'book', ...result })} />
      )

    case 'other':
      return null
  }
}
