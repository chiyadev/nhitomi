import React, { useState, useContext } from 'react'
import { Modal, Button, Form, Input, Select } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { ObjectType } from '../Client'
import TextArea from 'antd/lib/input/TextArea'
import { ClientContext } from '../ClientContext'
import { NotificationContext } from '../NotificationContext'

export const NewButton = () => {
  const client = useContext(ClientContext)
  const { notification: { error } } = useContext(NotificationContext)

  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  const [type, setType] = useState(ObjectType.Book)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  const cancel = () => {
    if (loading) return

    setOpen(false)
  }

  const submit = async () => {
    if (loading) return

    setLoading(true)

    try {
      await client.collection.createCollection({ createCollectionRequest: { type, collection: { name, description } } })

      setOpen(false)
    }
    catch (e) {
      error(e)
    }
    finally {
      setLoading(false)
    }
  }

  return <>
    <Button icon={<PlusOutlined />} onClick={() => setOpen(true)}>
      <span><FormattedMessage id='collectionListing.new.button' /></span>
    </Button>

    <Modal
      title={<FormattedMessage id='collectionListing.new.title' />}
      visible={open}
      onOk={submit}
      onCancel={cancel}
      afterClose={() => {
        setName('')
        setDescription('')
      }}
      closable={false}
      footer={<>
        <Button onClick={cancel}>
          <span><FormattedMessage id='collectionListing.new.cancel' /></span>
        </Button>
        <Button type='primary' loading={loading} onClick={submit}>
          <span><FormattedMessage id='collectionListing.new.ok' /></span>
        </Button>
      </>}>

      <Form
        layout='horizontal'
        labelCol={{ span: 5 }}
        wrapperCol={{ span: 19 }}>

        <Form.Item label={<FormattedMessage id='collectionListing.new.type' />} required>
          <Select
            value={type}
            onChange={setType}
            options={Object.values(ObjectType).map(type => ({
              label: <FormattedMessage id={`objectTypes.${type}`} />,
              value: type,
              disabled: type !== ObjectType.Book
            }))} />
        </Form.Item>

        <Form.Item label={<FormattedMessage id='collectionListing.new.name' />} required>
          <Input
            autoFocus
            value={name}
            onChange={({ target: { value } }) => setName(value)} />
        </Form.Item>

        <Form.Item label={<FormattedMessage id='collectionListing.new.description' />}>
          <TextArea
            rows={4}
            value={description}
            onChange={({ target: { value } }) => setDescription(value)} />
        </Form.Item>
      </Form>
    </Modal>
  </>
}
