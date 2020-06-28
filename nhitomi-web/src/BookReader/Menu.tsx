import { useContext } from 'react'
import { BookReaderContext } from '.'
import React from 'react'
import { Drawer, Collapse, Descriptions } from 'antd'
import { useShortcut } from '../shortcuts'
import { LayoutContext } from '../LayoutContext'
import { FormattedMessage } from 'react-intl'
import { TimeDisplay } from '../TimeDisplay'
import { BookTagList, TagDisplay } from '../Tags'

export const Menu = () => {
  const { book, content, menu, setMenu } = useContext(BookReaderContext)
  const { width } = useContext(LayoutContext)

  useShortcut('bookReaderMenuKey', () => setMenu(true))

  return (
    <Drawer
      title={<div style={{ width: '100%', whiteSpace: 'nowrap', textOverflow: 'ellipsis', overflow: 'hidden' }}>{book.primaryName}</div>}
      placement='right'
      visible={menu}
      onClose={() => setMenu(false)}
      width={Math.min(600, width)}>

      <Collapse defaultActiveKey={['info']}>
        <Collapse.Panel
          key='info'
          header={<FormattedMessage id='bookReader.menu.info.header' />}>

          <Descriptions size='middle' column={2}>
            <Descriptions.Item span={2} label='ID'>{book.id}/{content.id}</Descriptions.Item>

            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.uploadedTime' />}><TimeDisplay time={book.createdTime} /></Descriptions.Item>
            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.updatedTime' />}><TimeDisplay time={book.updatedTime} /></Descriptions.Item>

            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.category' />}><FormattedMessage id={`bookCategories.${book.category}`} /></Descriptions.Item>
            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.rating' />}><FormattedMessage id={`materialRatings.${book.rating}`} /></Descriptions.Item>

            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.pages' />}>{content.pageCount}</Descriptions.Item>
            <Descriptions.Item label={<FormattedMessage id='bookReader.menu.info.notes' />}>{Object.values(content.notes).length}</Descriptions.Item>

            <Descriptions.Item span={2} label={<FormattedMessage id='bookReader.menu.info.primaryName' />}>{book.primaryName}</Descriptions.Item>
            <Descriptions.Item span={2} label={<FormattedMessage id='bookReader.menu.info.englishName' />}>{book.englishName}</Descriptions.Item>
            <Descriptions.Item span={2} label={<FormattedMessage id='bookReader.menu.info.source' />}><a href={content.sourceUrl} target='_blank' rel='noopener noreferrer'>{content.sourceUrl}</a></Descriptions.Item>

            <Descriptions.Item span={2} label={<FormattedMessage id='bookReader.menu.info.tags' />}>
              <div>
                {BookTagList.flatMap(type => book.tags[type]?.map(value => (
                  <TagDisplay key={`${type}:${value}`} tag={type} value={value} />
                )))}
              </div>
            </Descriptions.Item>
          </Descriptions>
        </Collapse.Panel>

        <Collapse.Panel
          key='scrapers'
          header={<FormattedMessage id='bookReader.menu.keys.header' />}>

        </Collapse.Panel>
      </Collapse>
    </Drawer>
  )
}
