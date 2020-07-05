import React from 'react'
import { User, ObjectType, Book, Collection } from '../Client'
import { Prefetch, usePrefetch, PrefetchLinkProps, PrefetchLink } from '../Prefetch'
import { useScrollShortcut } from '../shortcuts'
import { useTabTitle } from '../hooks'
import { useIntl, FormattedMessage } from 'react-intl'
import { PageHeader } from 'antd'
import { FolderOutlined } from '@ant-design/icons'
import { BookGrid } from './BookGrid'
import { LayoutContent } from '../Layout'

type Fetched = {
  my: boolean
  user: User
  books: { collection: Collection, cover?: Book }[]
}

export function getCollectionListingPrefetch(id?: string): Prefetch<Fetched> {
  return {
    path: '/collections',

    func: async client => {
      let my: boolean
      let user: User
      let collections: Collection[]

      // retrieve user info and collections
      if (id)
        [my, user, collections] = await Promise.all([false, client.user.getUser({ id }), client.user.getUserCollections({ id }).then(x => x.items)])

      else if (client.currentInfo.authenticated)
        [my, user, collections] = [true, client.currentInfo.user, await client.user.getUserCollections(client.currentInfo.user).then(x => x.items)]

      else
        throw Error('Unauthenticated')

      // retrieve cover books
      const bookCollections = collections.filter(c => c.type === ObjectType.Book)
      const bookCoverIds = bookCollections.map(c => c.items[0]).filter(x => x)
      const bookCovers = bookCoverIds.length ? (await client.book.getBooks({ getBookManyRequest: { ids: bookCoverIds } })).reduce((x, book) => { x[book.id] = book; return x }, {} as { [key: string]: Book }) : {}
      const books = bookCollections.map(c => ({ collection: c, cover: bookCovers[c.items[0]] }))

      return { my, user, books }
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

const Loaded = ({ result: { my, user, books } }: { result: Fetched }) => {
  const { formatMessage } = useIntl()
  const title = formatMessage({ id: my ? 'collectionListing.header.title' : 'collectionListing.headerUser.title' }, { user: user.username })
  const sub = formatMessage({ id: my ? 'collectionListing.header.sub' : 'collectionListing.headerUser.sub' }, { user: user.username })

  useScrollShortcut()
  useTabTitle(title)

  return <>
    <PageHeader
      avatar={{ icon: <FolderOutlined />, shape: 'square' }}
      title={title}
      subTitle={sub} />

    <LayoutContent>
      <BookGrid items={books} />
    </LayoutContent>
  </>
}
