import { ScraperInfo } from '../Client'
import React from 'react'
import { List, Card } from 'antd'
import { SourceIcon } from '../SourceButton'

export const Sources = ({ sources }: {
  sources: ScraperInfo[]
}) => (
    <List
      grid={{ gutter: 8, xs: 1, sm: 2, md: 3, lg: 4, xl: 5, xxl: 6 }}
      dataSource={sources}
      renderItem={({ type, name, url }) => (
        <a href={url} target='_blank' rel='noopener noreferrer'>
          <List.Item>
            <Card hoverable>
              <Card.Meta
                avatar={<SourceIcon type={type} style={{ width: '3em' }} />}
                title={name}
                description={<a href={url} target='_blank' rel='noopener noreferrer'>{url}</a>} />
            </Card>
          </List.Item>
        </a>
      )} />
  )
