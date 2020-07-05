import React, { useContext } from 'react'
import { Modal, Button, Input, InputNumber } from 'antd'
import { BookReaderContext } from '.'
import { useShortcut } from '../shortcuts'
import { FormattedMessage } from 'react-intl'
import { ArrowRightOutlined } from '@ant-design/icons'

export const JumpModal = () => {
  const { fetch, currentPage, setCurrentPage, jump, setJump } = useContext(BookReaderContext)

  useShortcut('bookReaderJumpKey', () => setJump(true))

  return (
    <Modal
      title={<FormattedMessage id='bookReader.jump.header' />}
      centered
      visible={jump}
      onOk={() => setJump(false)}
      onCancel={() => setJump(false)}
      footer={null}
      width={400}>

      <Input.Group compact style={{
        display: 'flex',
        flexDirection: 'row',
        width: '100%'
      }}>
        <InputNumber
          ref={(r?: HTMLElement) => requestAnimationFrame(() => jump && r?.focus())}
          style={{ flex: 1 }}
          size='large'
          defaultValue={currentPage.pagePassive + 1}
          min={1}
          max={fetch.images.length}
          placeholder={`1 ~ ${fetch.images.length}`}
          onChange={e => {
            if (typeof e === 'number')
              setCurrentPage({ ...currentPage, pageInduced: e - 1 })
          }}
          onKeyDown={e => {
            switch (e.keyCode) {
              // enter
              case 13:
                setJump(false)
                e.preventDefault()
                break
            }
          }} />

        <Button
          type='primary'
          size='large'
          icon={<ArrowRightOutlined />}
          onClick={() => setJump(false)} />
      </Input.Group>
    </Modal>
  )
}
