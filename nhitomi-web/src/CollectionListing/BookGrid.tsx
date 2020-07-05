import React, { useContext, ReactElement, useState } from 'react'
import { List, Card, Typography, Empty, Tooltip } from 'antd'
import { ListGridType } from 'antd/lib/list'
import { LayoutContext } from '../LayoutContext'
import { Collection, Book, User, SpecialCollection } from '../Client'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { CollectionContentLink } from '../CollectionContent'
import { FormattedMessage } from 'react-intl'
import { HeartFilled, EyeFilled } from '@ant-design/icons'

const gridGutter = 6
const gridLayout: ListGridType = {
  xs: 2,
  sm: 3,
  md: 3,
  lg: 4,
  xl: 5,
  xxl: 7
}

export const BookGrid = ({ user, items }: { user: User, items: { collection: Collection, cover?: Book }[] }) => {
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
      renderItem={({ collection, cover }) => (
        <Item
          collection={collection}
          cover={cover}
          special={Object.entries(user.specialCollections?.book || {}).find(([, id]) => id === collection.id)?.[0] as SpecialCollection} />
      )} />
  )
}

const Item = ({ collection, cover, special }: { collection: Collection, cover?: Book, special?: SpecialCollection }) => {
  const client = useContext(ClientContext)
  const coverContent = cover && (cover.contents.find(c => client.currentInfo.authenticated && c.language === client.currentInfo.user.language) || cover.contents[0])

  return (
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

              {special && <SpecialCollectionIcon type={special} />}
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
  )
}

const SpecialCollectionIcon = ({ type }: { type: SpecialCollection }) => {
  let icon: ReactElement

  switch (type) {
    case SpecialCollection.Favorites:
      icon = <HeartFilled />
      break

    case SpecialCollection.Later:
      icon = <EyeFilled />
      break
  }

  const [hovered, setHovered] = useState(false)

  icon = (
    <span
      children={icon}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)} />
  )

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
      <Tooltip children={icon} title={<FormattedMessage id={`specialCollections.${type}`} />} />
    </div>
  )
}
