import React, { Dispatch, useCallback, useContext, useRef, useLayoutEffect, useMemo } from 'react'
import { useTabTitle } from '../hooks'
import { LayoutContent } from '../Layout'
import { Fetched } from '.'
import { AffixGradientPageHeader } from '../BookListing/AffixGradientPageHeader'
import { PageHeader } from 'antd'
import { FormattedMessage } from 'react-intl'
import { FolderOpenOutlined } from '@ant-design/icons'
import { SearchQuery, SearchResult, SearchManager } from '../BookListing/searchManager'
import { Client, Collection, Book } from '../Client'
import { ClientContext } from '../ClientContext'
import { ProgressContext } from '../Progress'
import { LocaleContext } from '../LocaleProvider'
import { NotificationContext } from '../NotificationContext'
import { BookListingContext } from '../BookListing'
import { Grid as BookGrid } from '../BookListing/Grid'
import { useScrollShortcut } from '../shortcuts'

export const CollectionContentBookView = ({ fetched, dispatch }: { fetched: Fetched, dispatch: Dispatch<Fetched> }) => {
  const { collection, result } = fetched

  useScrollShortcut()
  useTabTitle(collection.name)

  return <>
    <AffixGradientPageHeader>
      <PageHeader
        avatar={{ icon: <FolderOpenOutlined />, shape: 'square' }}
        title={collection.name}
        subTitle={collection.description || <FormattedMessage id='collectionContent.nodesc' />} />
    </AffixGradientPageHeader>

    <LayoutContent>
      <Grid
        collection={collection}
        result={result}
        dispatch={useCallback(s => dispatch({ ...fetched, result: s }), [dispatch, fetched])} />
    </LayoutContent>
  </>
}

export type BookCollectionLoadQuery = SearchQuery
export type BookCollectionLoadResult = SearchResult

// book collection is similar to book listing, only without querying.
// we simply override book listing's search manager for code reuse.
export class BookCollectionManager extends SearchManager {
  constructor(client: Client, readonly collection: Collection) { super(client) }

  async search(query: BookCollectionLoadQuery, offset?: number): Promise<{ items: Book[], total: number }> {
    const ids = this.collection.items.slice(offset, 50)

    return {
      items: await this.client.book.getBooks({ getBookManyRequest: { ids } }),
      total: this.collection.items.length
    }
  }
}

const Grid = ({ collection, result, dispatch }: { collection: Collection, result: BookCollectionLoadResult, dispatch: Dispatch<BookCollectionLoadResult> }) => {
  const client = useContext(ClientContext)
  const { start, stop } = useContext(ProgressContext)
  const { locale, setLocale } = useContext(LocaleContext)
  const { notification: { error } } = useContext(NotificationContext)

  const manager = useRef(new BookCollectionManager(client, collection)).current

  useLayoutEffect(() => {
    const onloading = (loading: boolean) => { if (loading) start(); else stop() }
    const onquery = (query: BookCollectionLoadQuery) => setLocale(query.language)

    manager.on('loading', onloading)
    manager.on('query', onquery)
    manager.on('result', dispatch)
    manager.on('failed', error)

    return () => {
      manager.off('loading', onloading)
      manager.off('query', onquery)
      manager.off('result', dispatch)
      manager.off('failed', error)
    }
  }, [error, locale, manager, setLocale, start, stop, dispatch])

  useLayoutEffect(() => manager.replace(manager.query, result), [manager, result])

  return (
    <BookListingContext.Provider value={useMemo(() => ({ manager }), [manager])}>
      <BookGrid />
    </BookListingContext.Provider>
  )
}
