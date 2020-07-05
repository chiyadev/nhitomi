import React, { useState } from 'react'
import { Menu } from 'antd'
import { Book } from '../Client'
import { FileTextOutlined } from '@ant-design/icons'
import { Details } from './Details'
import { FormattedMessage } from 'react-intl'

export const ReaderMenu = ({ book }: { book: Book }) => {
  const [details, setDetails] = useState(false)

  return (
    <Menu>
      <Details open={details} setOpen={setDetails} />

      <Menu.Item
        icon={<FileTextOutlined />}
        onClick={() => setDetails(true)}>

        <FormattedMessage id='bookReader.menu.details' />
      </Menu.Item>
    </Menu>
  )
}
