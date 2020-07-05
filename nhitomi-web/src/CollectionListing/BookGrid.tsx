import React, { useContext } from 'react'
import { List, Card, Typography, Empty } from 'antd'
import { ListGridType } from 'antd/lib/list'
import { LayoutContext } from '../LayoutContext'
import { Collection, Book } from '../Client'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { CollectionContentLink } from '../CollectionContent'
import { FormattedMessage } from 'react-intl'

const gridGutter = 6
const gridLayout: ListGridType = {
  xs: 2,
  sm: 3,
  md: 3,
  lg: 4,
  xl: 5,
  xxl: 7
}

export const BookGrid = ({ items }: { items: { collection: Collection, cover?: Book }[] }) => {
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
      renderItem={({ collection, cover }) => <Item collection={collection} cover={cover} />} />
  )
}

const Item = ({ collection, cover }: { collection: Collection, cover?: Book }) => {
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
              src={cover && coverContent ? (() => client.book.getBookImage({ id: cover.id, contentId: coverContent.id, index: -1 })) : undefined} />
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
