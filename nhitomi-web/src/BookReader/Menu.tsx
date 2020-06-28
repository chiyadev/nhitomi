import { useContext, useLayoutEffect } from 'react'
import { BookReaderContext } from '.'
import React from 'react'
import { Drawer, Collapse, Descriptions, Tabs, Radio } from 'antd'
import { useShortcut } from '../shortcuts'
import { LayoutContext } from '../LayoutContext'
import { FormattedMessage } from 'react-intl'
import { TimeDisplay } from '../TimeDisplay'
import { BookTagList, TagDisplay } from '../Tags'
import { SourceIcon } from '../SourceButton'
import { ClientContext } from '../ClientContext'
import { languageNames } from '../LocaleProvider'
import { LanguageType } from '../Client'
import { KeySettings } from '../Settings/KeySettings'

export const Menu = () => {
  const client = useContext(ClientContext)
  const { book, content, setContent, menu, setMenu } = useContext(BookReaderContext)
  const { width } = useContext(LayoutContext)

  useShortcut('bookReaderMenuKey', () => setMenu(true))

  // manually lose focus after drawer close
  useLayoutEffect(() => { if (!menu) (document.activeElement as HTMLElement)?.blur() }, [menu])

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
          key='sources'
          header={<FormattedMessage id='bookReader.menu.sources.header' />}>

          <Tabs size='small' defaultActiveKey="2">
            {book.contents.map(c => c.source).filter((s, i, a) => a.indexOf(s) === i).map(source => (
              <Tabs.TabPane
                key={source}
                tab={<>
                  <SourceIcon type={source} style={{ height: '2em', marginRight: 3 }} />
                  <span>{client.currentInfo.scrapers.find(s => s.type === source)?.name}</span>
                </>}>

                <Radio.Group value={content} onChange={({ target: { value } }) => setContent(value)}>
                  {book.contents
                    .filter(c => c.source === source)
                    .sort((a, b) => { const langs = Object.values(LanguageType); return langs.indexOf(a.language) - langs.indexOf(b.language) })
                    .map(content => (
                      <Radio
                        key={content.id}
                        value={content}
                        style={{
                          display: 'block',
                          height: '30px',
                          lineHeight: '30px'
                        }}>

                        <span>{languageNames[content.language]} — <a href={content.sourceUrl} target='_blank' rel='noopener noreferrer'>{content.sourceUrl}</a></span>
                      </Radio>
                    ))}
                </Radio.Group>
              </Tabs.TabPane>
            ))}
          </Tabs>
        </Collapse.Panel>

        <Collapse.Panel
          key='scrapers'
          header={<FormattedMessage id='bookReader.menu.keys.header' />}>

          <KeySettings prefix='bookReader' />
        </Collapse.Panel>
      </Collapse>
    </Drawer>
  )
}
