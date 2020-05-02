import { CloseOutlined } from '@ant-design/icons'
import { Card, List, Popover, Typography } from 'antd'
import { ListGridType } from 'antd/lib/list'
import React, { useContext, useEffect, useMemo, useRef } from 'react'
import { useIntl } from 'react-intl'
import { useMouseHovered } from 'react-use'
import { BookTagList, CategoryDisplay, LanguageTypeDisplay, MaterialRatingDisplay, TagDisplay } from '../Tags'
import { Book, BookContent } from '../Client'
import { LayoutContext } from '../LayoutContext'
import { ClientContext } from '../ClientContext'
import { AsyncImage } from '../AsyncImage'
import { BookReaderLink } from '../BookReader'
import { addQueryTag } from './queryHelper'
import { BookQueryContext } from '.'

const gridGutter = 2
const gridLayout: ListGridType = {
  gutter: gridGutter,
  xs: 2,
  sm: 3,
  md: 3,
  lg: 4,
  xl: 6,
  xxl: 8
}

export const GridListing = ({ items, selected, setSelected }: {
  items: Book[]
  setItems: (items: Book[]) => void

  selected?: string
  setSelected: (id?: string) => void
}) => {
  // optimization
  const setSelectedFuncs = useMemo(() => items.map(book => (v: boolean) => setSelected(v ? book.id : undefined)), [items, setSelected])

  return useMemo(() =>
    <List<Book>
      grid={gridLayout}
      dataSource={items}
      rowKey={book => book.id}
      renderItem={(book, i) =>
        <Item
          book={book}
          selected={book.id === selected}
          setSelected={setSelectedFuncs[i]} />} />,
    [
      items,
      selected,
      setSelectedFuncs
    ])
}

const Item = ({ book, selected, setSelected }: {
  book: Book
  selected: boolean
  setSelected: (selected: boolean) => void
}) => {
  const content = book.contents[0]

  const { width: windowWidth, height: windowHeight, mobile } = useContext(LayoutContext)
  const ref = useRef<HTMLDivElement>(null)

  // scroll into view on when selected
  useEffect(() => {
    if (selected)
      ref.current?.scrollIntoView({
        behavior: 'smooth',
        block: mobile ? 'start' : 'nearest',
        inline: mobile ? 'start' : 'nearest'
      })
  }, [selected, mobile, windowWidth, windowHeight])

  return useMemo(() =>
    <List.Item style={{
      marginTop: 0,
      marginBottom: gridGutter
    }}>
      <Popover
        trigger='click'
        placement={mobile ? 'bottom' : 'rightTop'}
        visible={selected || false}
        onVisibleChange={setSelected}
        title={<OverlayTitle book={book} content={content} onClose={() => setSelected(false)} />}
        content={<Overlay book={book} content={content} />}
        overlayStyle={{
          width: mobile ? '100%' : undefined,
          maxWidth: mobile ? undefined : windowWidth / 2
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
    [
      book,
      content,
      mobile,
      selected,
      setSelected,
      windowWidth
    ])
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
      loadingDisabled
      fluid
      src={() => client.book.getBookThumbnail({ id, contentId })}
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
  const { query, setQuery } = useContext(BookQueryContext)

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
      {BookTagList.flatMap(tag => tags[tag]?.map(value =>
        <TagDisplay
          key={`${tag}:${value}`}
          tag={tag}
          value={value}
          onClick={() => setQuery(addQueryTag(query, tag, value))} />))}
    </p>
  </>
}
