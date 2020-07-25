import React, { Dispatch, useCallback, useContext, useRef, useLayoutEffect, useMemo } from 'react'
import { useTabTitle } from '../hooks'
import { LayoutContent } from '../Layout'
import { Fetched, getCollectionContentPrefetch, CollectionContentLink } from '.'
import { AffixGradientPageHeader } from '../BookListing/AffixGradientPageHeader'
import { PageHeader, Dropdown, Button, Menu, Modal } from 'antd'
import { FormattedMessage, useIntl } from 'react-intl'
import { FolderOpenOutlined, EllipsisOutlined, DeleteOutlined, ExclamationCircleOutlined, SnippetsOutlined, HeartOutlined, EyeOutlined } from '@ant-design/icons'
import { SearchQuery, SearchResult, SearchManager } from '../BookListing/searchManager'
import { Client, Collection, Book, CollectionInsertPosition, SpecialCollection } from '../Client'
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
import { getCollectionListingPrefetch, CollectionListingLink } from '../CollectionListing'

export const CollectionContentBookView = ({ fetched, dispatch }: { fetched: Fetched, dispatch: Dispatch<Fetched> }) => {
  const client = useContext(ClientContext)
  const { formatMessage } = useIntl()
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
        breadcrumb={{
          routes: [{
            path: 'listing',
            breadcrumbName: formatMessage({ id: 'collectionListing.header.title' })
          }, {
            path: 'collection',
            breadcrumbName: collection.name
          }],
          itemRender: ({ path, breadcrumbName }) => {
            switch (path) {
              case 'listing': return <CollectionListingLink>{breadcrumbName}</CollectionListingLink>
              case 'collection': return <CollectionContentLink id={collection.id}>{breadcrumbName}</CollectionContentLink>
            }
          }
        }}
        extra={(
          <Dropdown placement='bottomRight' overlay={menu}>
            <Button shape='circle' type='text'>
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
  constructor(client: Client, public collection: Collection) { super(client) }

  async search(query: BookCollectionLoadQuery, offset?: number): Promise<{ items: Book[], total: number }> {
    const ids = this.collection.items.slice(offset, (offset || 0) + 50)

    if (!ids.length)
      return {
        items: [],
        total: this.collection.items.length
      }

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
  const { notification: { error }, alert: { success } } = useContext(NotificationContext)

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

  const additionalMenus = useCallback((book: Book) => {
    return [
      <Menu.Item
        danger
        icon={<DeleteOutlined />}
        onClick={async () => {
          start()

          try {
            manager.collection = await client.collection.removeCollectionItems({ id: collection.id, collectionItemsRequest: { items: [book.id] } })

            manager.result = {
              ...manager.result,
              items: manager.result.items.filter(b => b.id !== book.id),
              total: manager.collection.items.length
            }

            success(<FormattedMessage id='collectionContent.bookMenu.remove.success' />)
          }
          catch (e) {
            error(e)
          }
          finally {
            stop()
          }
        }}>

        <FormattedMessage id='collectionContent.bookMenu.remove.text' />
      </Menu.Item>
    ]
  }, [start, manager.collection, manager.result, client.collection, collection.id, success, error, stop])

  return (
    <BookListingContext.Provider value={useMemo(() => ({ manager, additionalMenus }), [manager, additionalMenus])}>
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

  const special = client.currentInfo.authenticated && getCollectionSpecialType(client.currentInfo.user, collection)

  const setAsSpecial = async (type: SpecialCollection) => {
    if (!client.currentInfo.authenticated) return

    start()

    try {
      client.currentInfo = {
        ...client.currentInfo,

        user: await client.user.updateUser({
          id: client.currentInfo.user.id,
          userBase: {
            ...client.currentInfo.user,
            specialCollections: {
              ...client.currentInfo.user.specialCollections,
              book: {
                ...client.currentInfo.user.specialCollections?.book,
                [type]: collection.id
              }
            }
          }
        })
      }

      success(<FormattedMessage id={`collectionContent.menu.${type}Set.success`} />)
    }
    catch (e) {
      error(e)
    }
    finally {
      stop()
    }
  }

  return (
    <Menu>
      <Menu.Item
        icon={<HeartOutlined />}
        disabled={!!special}
        onClick={() => setAsSpecial(SpecialCollection.Favorites)}>

        <FormattedMessage id='collectionContent.menu.favoritesSet.text' />
      </Menu.Item>

      <Menu.Item
        icon={<EyeOutlined />}
        disabled={!!special}
        onClick={() => setAsSpecial(SpecialCollection.Later)}>

        <FormattedMessage id='collectionContent.menu.laterSet.text' />
      </Menu.Item>

      <Menu.Divider />

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