import { Card, List, Typography, Spin, Empty } from 'antd'
import { ListGridType } from 'antd/lib/list'
import React, { useContext, useMemo, useRef, useLayoutEffect, useState } from 'react'
import { useMouseHovered } from 'react-use'
import { Book, BookContent } from '../Client'
import { LayoutContext } from '../LayoutContext'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { BookReaderLink } from '../BookReader'
import VisibilitySensor from 'react-visibility-sensor'
import { BookListingContext } from '.'
import { useUpdateOnEvent } from '../hooks'
import { presetPrimaryColors } from '@ant-design/colors'
import { Panel } from './Panel'
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

export const GridListing = () => {
  const { manager } = useContext(BookListingContext)
  const { width, getBreakpoint } = useContext(LayoutContext)
  const listWidth = manager.result.selected ? width * 0.6 : width

  const listRef = useRef<HTMLDivElement>(null)
  const [panelPos, setPanelPos] = useState(0)

  const selected = manager.result.selected?.book.id

  useUpdateOnEvent(manager, 'result')
  useLayoutEffect(() => { requestAnimationFrame(() => setPanelPos(Math.max(0, window.scrollY - (listRef.current?.offsetTop || 0)))) }, [selected, width])

  const list = useMemo(() => {
    if (!manager.result.items.length)
      return <Empty description={<FormattedMessage id='bookListing.empty' />} />

    return <>
      <List
        grid={{
          gutter: gridGutter,
          column: gridLayout[getBreakpoint(listWidth)]
        }}
        dataSource={manager.result.items}
        rowKey={book => book.id}
        renderItem={book => <Item book={book} />} />

      <FurtherLoader />
    </>
  }, [getBreakpoint, listWidth, manager.result.items])

  return <>
    <div ref={listRef} children={list} style={{
      display: 'inline-block',
      width: (listWidth / width) * 100 + '%'
    }} />

    {manager.result.selected && <div style={{
      marginTop: panelPos,
      width: (1 - listWidth / width) * 100 + '%',
      float: 'right',
      paddingLeft: gridGutter
    }}>
      <Panel />
    </div>}
  </>
}

const FurtherLoader = () => {
  const loading = useRef(false)
  const { manager } = useContext(BookListingContext)
  const { height } = useContext(LayoutContext)

  useUpdateOnEvent(manager, 'result')

  if (manager.result.end) {
    loading.current = false

    return null
  }

  return <VisibilitySensor intervalCheck scrollCheck resizeCheck partialVisibility offset={{ bottom: -height }} onChange={async value => {
    const beginLoad = !loading.current && value

    loading.current = value

    if (!beginLoad)
      return

    try {
      while (loading.current) {
        await manager.further()
        await new Promise(r => setTimeout(r)) // visibility sensor would move, so we wait for loading.current to update
      }
    }
    finally {
      loading.current = false
    }
  }}>
    <Spin>
      <div style={{
        width: '100%',
        height: '10em'
      }} />
    </Spin>
  </VisibilitySensor>
}

const Item = ({ book }: { book: Book }) => {
  const { manager } = useContext(BookListingContext)
  const { width, breakpoint } = useContext(LayoutContext)

  useUpdateOnEvent(manager, 'result')

  const ref = useRef<HTMLDivElement>(null)

  // use content of user language
  const selected = manager.result.selected?.book.id === book.id
  const content = book.contents.find(c => c.id === manager.result.selected?.content.id) || book.contents.find(c => c.language === manager.result.query.language) || book.contents[0]

  const lastSelected = useRef<string>()
  const lastWidth = useRef(width)

  // scroll into view when selected, or selection is lost (not switched to another), or when window is resized
  useLayoutEffect(() => {
    const isSelected = (!lastSelected.current && manager.result.selected?.book.id === book.id)
    const isUnselected = (lastSelected.current === book.id && !manager.result.selected)
    const isResized = (manager.result.selected?.book.id === book.id && width !== lastWidth.current)

    if (isSelected || isUnselected || isResized)
      ref.current?.scrollIntoView({ block: 'center', inline: 'center' })

    lastSelected.current = manager.result.selected?.book.id
    lastWidth.current = width
  }, [book.id, manager.result.selected, width])

  return useMemo(() => (
    <List.Item style={{
      marginTop: 0,
      marginBottom: gridGutter
    }}>
      <div ref={ref} style={{ scrollMargin: '1em' }}>
        <BookReaderLink
          id={book.id}
          contentId={content.id}
          disabled={!breakpoint && !selected} // on mobile, don't show information panel
          onClick={breakpoint ? undefined : () => manager.result = { ...manager.result, selected: { book, content } }}>

          <Card
            size='small'
            style={{
              ...(!selected ? undefined : {
                border: 'solid',
                borderColor: presetPrimaryColors.blue,
                borderWidth: gridGutter / 2,
                borderRadius: gridGutter / 2,
                margin: 1 - gridGutter / 2,
                zIndex: 1
              }),
              transition: 'border-color 0.2s',
              overflow: 'hidden'
            }}
            cover={<Cover book={book} content={content} selected={selected} />}>

            <Card.Meta description={(
              <div style={{
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                overflow: 'hidden'
              }}>
                <Typography.Text strong>{book.primaryName}</Typography.Text>
              </div>
            )} />
          </Card>
        </BookReaderLink>
      </div>
    </List.Item>
  ), [book, content, selected, breakpoint]) // eslint-disable-line react-hooks/exhaustive-deps
}

const Cover = ({ book: { id }, content: { id: contentId }, selected }: { book: Book, content: BookContent, selected: boolean }) => {
  const client = useContext(ClientContext)
  const { breakpoint } = useContext(LayoutContext)

  const ref = useRef<HTMLDivElement>(null)
  const imageRef = useRef<HTMLImageElement>(null)

  let { elX, elY } = useMouseHovered(ref, { whenHovered: true, bound: true })
  let scale = 1

  if (selected && !breakpoint) {
    scale = 1.1
  }
  else {
    elX = 0
    elY = 0
  }

  return useMemo(() =>
    <AsyncImage
      key={`${id}/${contentId}`}
      ref={imageRef}
      width={5}
      height={7}
      resize='fill'
      fluid
      preloadScale={0.5}
      src={() => client.book.getBookImage({ id, contentId, index: -1 })}
      style={{
        transition: 'left 0.1s, top 0.1s, width 0.1s, height 0.1s',
        width: scale * 100 + '%',
        height: scale * 100 + '%',
        left: elX * (1 - scale),
        top: elY * (1 - scale)
      }}
      wrapperRef={ref} />,
    [
      client.book,
      id,
      contentId,
      elX,
      elY,
      scale
    ])
}
