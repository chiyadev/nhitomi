import React, { useContext, useState } from 'react'
import { List, Card, Typography, Empty, Tooltip, Dropdown } from 'antd'
import { ListGridType } from 'antd/lib/list'
import { LayoutContext } from '../LayoutContext'
import { Collection, Book, User, SpecialCollection } from '../Client'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { CollectionContentLink } from '../CollectionContent'
import { FormattedMessage } from 'react-intl'
import { HeartFilled, EyeFilled, HeartOutlined, EyeOutlined } from '@ant-design/icons'
import { CollectionContentBookMenu } from '../CollectionContent/BookView'

const gridGutter = 6
const gridLayout: ListGridType = {
  xs: 2,
  sm: 3,
  md: 3,
  lg: 4,
  xl: 5,
  xxl: 7
}

export function getCollectionSpecialType(user: User, collection: Collection) {
  return Object.entries(user.specialCollections?.[collection.type] || {}).find(([, id]) => id === collection.id)?.[0] as SpecialCollection | undefined
}

export const BookGrid = ({ id, user, items }: { id?: string, user: User, items: { collection: Collection, cover?: Book }[] }) => {
  const { width, getBreakpoint } = useContext(LayoutContext)

  if (!items.length)
    return <Empty description={<FormattedMessage id='collectionListing.empty' />} />

  return (
    <List
      grid={{
        gutter: gridGutter,
        column: gridLayout[getBreakpoint(width)]
      }}
      dataSource={items}
      rowKey={({ collection }) => collection.id}
      renderItem={({ collection, cover }) => <Item id={id} collection={collection} cover={cover} special={getCollectionSpecialType(user, collection)} />} />
  )
}

const Item = ({ id, collection, cover, special }: { id?: string, collection: Collection, cover?: Book, special?: SpecialCollection }) => {
  const client = useContext(ClientContext)
  const coverContent = cover && (cover.contents.find(c => client.currentInfo.authenticated && c.language === client.currentInfo.user.language) || cover.contents[0])

  const menu = CollectionContentBookMenu({ collection, onDeleteListingId: id })

  return (
    <Dropdown trigger={['contextMenu']} overlay={menu}>
      <List.Item style={{
        marginTop: 0,
        marginBottom: gridGutter
      }}>
        <CollectionContentLink id={collection.id}>
          <Card
            size='small'
            cover={(
              <AsyncImage
                key={`${cover?.id}/${coverContent?.id}`}
                width={5}
                height={7}
                resize='fill'
                fluid
                preloadScale={0.5}
                src={cover && coverContent ? (() => client.book.getBookImage({ id: cover.id, contentId: coverContent.id, index: -1 })) : undefined}>

                {special && <SpecialCollectionIconOverlay type={special} />}
              </AsyncImage>
            )}>

            <Card.Meta description={(
              <div style={{
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                overflow: 'hidden'
              }}>
                <Typography.Text strong>{collection.name}</Typography.Text>
              </div>
            )} />
          </Card>
        </CollectionContentLink>
      </List.Item>
    </Dropdown>
  )
}

export const SpecialCollectionIcon = ({ type, filled }: { type: SpecialCollection, filled?: boolean }) => {
  switch (type) {
    case SpecialCollection.Favorites:
      return filled ? <HeartFilled /> : <HeartOutlined />

    case SpecialCollection.Later:
      return filled ? <EyeFilled /> : <EyeOutlined />
  }
}

const SpecialCollectionIconOverlay = ({ type }: { type: SpecialCollection }) => {
  const [hovered, setHovered] = useState(false)

  return (
    <div style={{
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      opacity: hovered ? 1 : 0.8,
      background: 'linear-gradient(to bottom right, black 0%, transparent 15%)',
      color: 'white',
      padding: 5,
      transition: 'opacity 0.2s'
    }}>
      <Tooltip title={<FormattedMessage id={`specialCollections.${type}`} />}>
        <span
          onMouseEnter={() => setHovered(true)}
          onMouseLeave={() => setHovered(false)}>

          <SpecialCollectionIcon type={type} filled />
        </span>
      </Tooltip>
    </div>
  )
}
