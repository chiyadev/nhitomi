import React, { ComponentProps } from 'react'
import { TypedPrefetchLinkProps, PrefetchLink, usePostfetch, PrefetchGenerator } from '../Prefetch'
import { Book, User, Collection, ObjectType } from 'nhitomi-api'
import { useClient, useClientInfo } from '../ClientManager'
import { PageContainer } from '../Components/PageContainer'

export type PrefetchResult = { user: User, books: { collection: Collection, cover?: Book }[] }
export type PrefetchOptions = { id: string }

export const useCollectionListingPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = ({ id }) => {
  const client = useClient()
  const { info, setInfo } = useClientInfo()

  return {
    destination: {
      path: `/users/${id}/collections`
    },

    fetch: async () => {
      const [user, collections] = await Promise.all([
        client.user.getUser({ id }),
        client.user.getUserCollections({ id }).then(x => x.items)
      ])

      if (info.authenticated && info.user.id === user.id)
        setInfo({ ...info, user })

      const bookCollections = collections.filter(c => c.type === ObjectType.Book)
      const bookCoverIds = bookCollections.map(c => c.items[0]).filter(x => x)
      const bookCovers = bookCoverIds.length ? (await client.book.getBooks({ getBookManyRequest: { ids: bookCoverIds } })).reduce((x, book) => { x[book.id] = book; return x }, {} as { [key: string]: Book }) : {}
      const books = bookCollections.map(c => ({ collection: c, cover: bookCovers[c.items[0]] }))

      return { user, books }
    }
  }
}

export const SelfCollectionListingLink = (props: Omit<ComponentProps<typeof CollectionListingLink>, 'id'>) => {
  const { info } = useClientInfo()

  if (info.authenticated)
    return <CollectionListingLink id={info.user.id} {...props} />

  return <>{props.children}</>
}

export const CollectionListingLink = ({ id, ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useCollectionListingPrefetch} options={{ id }} {...props} />
)

export const CollectionListing = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useCollectionListingPrefetch, options)

  if (!result)
    return null

  return (
    <PageContainer>
      <Loaded user={result.user} books={result.books} />
    </PageContainer>
  )
}

const Loaded = ({ user, books }: PrefetchResult) => {
  return <>
  </>
}
