import { CloseOutlined } from '@ant-design/icons'
import { Card, List, Popover, Typography, Spin, Empty } from 'antd'
import { ListGridType } from 'antd/lib/list'
import React, { useContext, useMemo, useRef, useState, useLayoutEffect, CSSProperties } from 'react'
import { useIntl } from 'react-intl'
import { useMouseHovered } from 'react-use'
import { BookTagList, CategoryDisplay, LanguageTypeDisplay, MaterialRatingDisplay, TagDisplay } from '../Tags'
import { Book, BookContent } from '../Client'
import { LayoutContext } from '../LayoutContext'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { BookReaderLink } from '../BookReader'
import VisibilitySensor from 'react-visibility-sensor'
import { BookListingContext } from '.'

const gridGutter = 2
const gridLayout: ListGridType = {
  gutter: gridGutter,
  xs: 2,
  sm: 3,
  md: 3,
  lg: 4,
  xl: 5,
  xxl: 7
}

export const GridListing = ({ selected, setSelected }: {
  selected?: string
  setSelected: (id?: string) => void
}) => {
  const { manager } = useContext(BookListingContext)
  const [items, setItems] = useState<Book[]>(manager.items)

  useLayoutEffect(() => {
    const set = (items: Book[]) => setItems(items.slice())

    manager.on('items', set)
    return () => { manager.off('items', set) }
  }, [manager])

  // optimization
  const setSelectedFuncs = useMemo(() => items.map(book => (v: boolean) => setSelected(v ? book.id : undefined)), [items, setSelected])

  const list = useMemo(() =>
    <List
      grid={gridLayout}
      dataSource={items}
      rowKey={book => book.id}
      renderItem={(book, i) => (
        <Item
          book={book}
          selected={book.id === selected}
          setSelected={setSelectedFuncs[i]} />
      )} />,
    [
      items,
      selected,
      setSelectedFuncs
    ])

  if (!items.length)
    return <Empty description='No results' />

  return <>
    {list}

    <FurtherLoader />
  </>
}

const FurtherLoader = () => {
  const { manager } = useContext(BookListingContext)
  const loading = useRef(false)

  const [end, setEnd] = useState(manager.end)

  useLayoutEffect(() => {
    const onend = (end: boolean) => {
      setEnd(end)
      loading.current = loading.current && !end
    }

    manager.on('end', onend)
    return () => { manager.off('end', onend) }
  }, [manager])

  if (end)
    return null

  return <VisibilitySensor partialVisibility onChange={async value => {
    const beginLoad = !loading.current && value

    loading.current = value

    if (!beginLoad)
      return

    try {
      while (loading.current)
        await manager.further()
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

const Item = ({ book, selected, setSelected, colStyle }: {
  book: Book
  selected: boolean
  setSelected: (selected: boolean) => void

  // this is a hack for https://github.com/ant-design/ant-design/issues/24553 until the merged fix is released
  colStyle?: CSSProperties
}) => {
  const content = book.contents[0]

  const { width: windowWidth, height: windowHeight, breakpoint } = useContext(LayoutContext)
  const ref = useRef<HTMLDivElement>(null)

  // scroll into view on when selected
  useLayoutEffect(() => {
    if (selected)
      ref.current?.scrollIntoView({
        behavior: 'smooth',
        block: breakpoint ? 'start' : 'nearest',
        inline: breakpoint ? 'start' : 'nearest'
      })
  }, [selected, breakpoint, windowWidth, windowHeight])

  return useMemo(() =>
    <List.Item colStyle={colStyle} style={{
      marginTop: 0,
      marginBottom: gridGutter
    }}>
      <Popover
        trigger='click'
        placement={breakpoint ? 'bottom' : 'rightTop'}
        visible={selected || false}
        onVisibleChange={setSelected}
        title={<OverlayTitle book={book} content={content} onClose={() => setSelected(false)} />}
        content={<Overlay book={book} content={content} />}
        overlayStyle={{
          width: breakpoint ? '100%' : undefined,
          maxWidth: breakpoint ? undefined : windowWidth / 2
        }}>

        <div ref={ref} style={{ scrollMargin: '2em' }}>
          <BookReaderLink
            id={book.id}
            contentId={content.id}
            disabled={!selected}>

            <Card
              bordered
              hoverable
              size='small'
              cover={<Cover book={book} content={content} selected={selected} />}>

              <Card.Meta description={<div style={{
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                overflow: 'hidden'
              }}>
                <Typography.Text strong>{book.primaryName}</Typography.Text>
              </div>} />
            </Card>
          </BookReaderLink>
        </div>
      </Popover>
    </List.Item>,
    [book, colStyle, content, breakpoint, selected, setSelected, windowWidth])
}

const Cover = ({ book: { id }, content: { id: contentId }, selected }: { book: Book, content: BookContent, selected: boolean }) => {
  const client = useContext(ClientContext)

  const ref = useRef<HTMLDivElement>(null)
  const imageRef = useRef<HTMLImageElement>(null)

  let { elX, elY } = useMouseHovered(ref, { whenHovered: true, bound: true })
  let scale = 1

  if (selected) {
    scale = 1.1
  }
  else {
    elX = 0
    elY = 0
  }

  return useMemo(() =>
    <AsyncImage
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

const OverlayTitle = ({ book, content, onClose }: { book: Book, content: BookContent, onClose: () => void }) => <>
  <BookReaderLink id={book.id} contentId={content.id}><strong>{book.primaryName}</strong></BookReaderLink>

  <Typography.Text style={{ float: 'right', marginLeft: '1em' }} type='secondary'>
    <CloseOutlined style={{ cursor: 'pointer' }} onClick={onClose} />
  </Typography.Text>

  {book.englishName && <>
    <br />
    <Typography.Text type='secondary'><small> {book.englishName}</small></Typography.Text>
  </>}
</>

const Overlay = ({ book: { createdTime, updatedTime, tags, category, rating }, content: { language } }: { book: Book, content: BookContent }) => {
  const { formatDate, formatTime } = useIntl()
  const { manager } = useContext(BookListingContext)

  return <>
    <h4>Information</h4>
    <p>
      <span>Uploaded: {formatDate(createdTime)} {formatTime(createdTime)}</span>
      <br />
      <span>Updated: {formatDate(updatedTime)} {formatTime(updatedTime)}</span>
    </p>
    <p>
      <LanguageTypeDisplay language={language} />
      <CategoryDisplay category={category} />
      <MaterialRatingDisplay rating={rating} />
    </p>

    <h4>Tags</h4>
    <p>
      {BookTagList.flatMap(type => tags[type]?.map(value =>
        <TagDisplay
          key={`${type}:${value}`}
          tag={type}
          value={value}
          onClick={() => manager.toggleTag({ type, value })} />))}
    </p>
  </>
}
