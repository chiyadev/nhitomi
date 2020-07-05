import React, { useState, useMemo } from 'react'
import { Menu } from 'antd'
import { Book, BookContent } from '../Client'
import { FileTextOutlined } from '@ant-design/icons'
import { Details } from './Details'
import { FormattedMessage } from 'react-intl'

export const ReaderMenu = ({ book, content, setContent }: {
  book: Book
  content: BookContent
  setContent: (content: BookContent) => void
}) => {
  const [details, setDetails] = useState(false)
  const detailsPanel = useMemo(() => <Details open={details} setOpen={setDetails} book={book} content={content} setContent={setContent} />, [book, content, details, setContent])

  return useMemo(() => (
    <Menu>
      {detailsPanel}

      <Menu.Item
        icon={<FileTextOutlined />}
        onClick={() => setDetails(true)}>

        <FormattedMessage id='bookReader.menu.details' />
      </Menu.Item>
    </Menu>
  ), [detailsPanel])
}
