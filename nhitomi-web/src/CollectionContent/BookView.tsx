import React, { Dispatch, useCallback, useContext, useRef, useLayoutEffect, useMemo } from 'react'
import { useTabTitle } from '../hooks'
import { LayoutContent } from '../Layout'
import { Fetched, getCollectionContentPrefetch } from '.'
import { AffixGradientPageHeader } from '../BookListing/AffixGradientPageHeader'
import { PageHeader, Dropdown, Button, Menu, Modal } from 'antd'
import { FormattedMessage } from 'react-intl'
import { FolderOpenOutlined, EllipsisOutlined, DeleteOutlined, ExclamationCircleOutlined, SnippetsOutlined } from '@ant-design/icons'
import { SearchQuery, SearchResult, SearchManager } from '../BookListing/searchManager'
import { Client, Collection, Book, CollectionInsertPosition } from '../Client'
import { ClientContext } from '../ClientContext'
import { ProgressContext } from '../Progress'
import { LocaleContext } from '../LocaleProvider'
import { NotificationContext } from '../NotificationContext'
import { BookListingContext } from '../BookListing'
import { Grid as BookGrid } from '../BookListing/Grid'
import { useScrollShortcut } from '../shortcuts'
import { getCollectionSpecialType, SpecialCollectionIcon } from '../CollectionListing/BookGrid'
import { AsyncEditableText } from '../AsyncEditableText'
import { usePrefetchExecutor } from '../Prefetch'
import { getCollectionListingPrefetch } from '../CollectionListing'

export const CollectionContentBookView = ({ fetched, dispatch }: { fetched: Fetched, dispatch: Dispatch<Fetched> }) => {
  const client = useContext(ClientContext)
  const { collection, result } = fetched

  const special = client.currentInfo.authenticated && getCollectionSpecialType(client.currentInfo.user, collection)

  useScrollShortcut()
  useTabTitle(collection.name)

  const menu = CollectionContentBookMenu({ collection })

  return <>
    <AffixGradientPageHeader>
      <PageHeader
        avatar={{
          icon: (special && <SpecialCollectionIcon type={special} />) || <FolderOpenOutlined />,
          shape: 'square'
        }}
        title={(
          <AsyncEditableText
            ignoreOffsets
            value={collection.name}
            onChange={async name => dispatch({ ...fetched, collection: await client.collection.updateCollection({ id: collection.id, collectionBase: { ...collection, name } }) })} />
        )}
        subTitle={(
          <AsyncEditableText
            ignoreOffsets
            value={collection.description || (special && <FormattedMessage id={`specialCollections.${special}`} />) || <FormattedMessage id='collectionContent.nodesc' />}
            onChange={async description => dispatch({ ...fetched, collection: await client.collection.updateCollection({ id: collection.id, collectionBase: { ...collection, description } }) })} />
        )}
        extra={(
          <Dropdown placement='bottomRight' overlay={menu}>
            <Button
              shape='circle'
              type='text'>

              <EllipsisOutlined style={{ fontSize: '1rem' }} />
            </Button>
          </Dropdown>
        )} />
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

export const CollectionContentBookMenu = ({ collection, onDeleteListingId }: {
  collection: Collection

  onDeleteListingId?: string
}) => {
  const client = useContext(ClientContext)
  const push = usePrefetchExecutor()
  const [modal, modalContexts] = Modal.useModal()
  const { start, stop } = useContext(ProgressContext)
  const { notification: { error }, alert: { success } } = useContext(NotificationContext)

  return (
    <Menu>
      <Menu.Item
        icon={<SnippetsOutlined />}
        onClick={async () => {
          start()

          try {
            let clone = await client.collection.createCollection({ createCollectionRequest: { type: collection.type, collection } })
            clone = await client.collection.addCollectionItems({ id: clone.id, addCollectionItemsRequest: { items: collection.items, position: CollectionInsertPosition.End } })

            await push(getCollectionContentPrefetch(clone.id))

            success(<FormattedMessage id='collectionContent.menu.clone.success' />)
          }
          catch (e) {
            error(e)
          }
          finally {
            stop()
          }
        }}>

        <FormattedMessage id='collectionContent.menu.clone.text' />
      </Menu.Item>

      <Menu.Item
        danger
        icon={<DeleteOutlined />}
        onClick={() => modal.confirm({
          title: <FormattedMessage id='collectionContent.menu.delete.title' values={{ collection: collection.name }} />,
          icon: <ExclamationCircleOutlined />,
          content: <FormattedMessage id='collectionContent.menu.delete.description' />,
          okType: 'danger',
          okText: <span><FormattedMessage id='collectionContent.menu.delete.yes' /></span>,
          cancelText: <span><FormattedMessage id='collectionContent.menu.delete.no' /></span>,
          onOk: async () => {
            try {
              await client.collection.deleteCollection({ id: collection.id })
              await push(getCollectionListingPrefetch(onDeleteListingId))

              success(<FormattedMessage id='collectionContent.menu.delete.success' />)
            }
            catch (e) {
              error(e)
            }
          },
          onCancel() {
            console.log('Cancel')
          }
        })}>

        <FormattedMessage id='collectionContent.menu.delete.text' />
      </Menu.Item>

      {modalContexts}
    </Menu >
  )
}
