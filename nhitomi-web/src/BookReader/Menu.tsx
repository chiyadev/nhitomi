import { useContext } from 'react'
import { BookReaderContext } from '.'
import React from 'react'
import { Drawer } from 'antd'
import { useShortcut } from '../shortcuts'
import { LayoutContext } from '../LayoutContext'

export const Menu = () => {
  const { book, menu, setMenu } = useContext(BookReaderContext)
  const { width } = useContext(LayoutContext)

  useShortcut('bookReaderMenuKey', () => setMenu(true))

  return (
    <Drawer
      title={<div style={{ width: '100%', whiteSpace: 'nowrap', textOverflow: 'ellipsis', overflow: 'hidden' }}>{book.primaryName}</div>}
      placement='right'
      visible={menu}
      onClose={() => setMenu(false)}
      width={Math.min(600, width)}>

      <p>Some contents...</p>
      <p>Some contents...</p>
      <p>Some contents...</p>
    </Drawer>
  )
}
