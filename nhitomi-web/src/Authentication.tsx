import React, { useContext, ReactNode } from 'react'
import { Modal, Typography, Button } from 'antd'
import { ClientContext } from './ClientContext'
import { HeartOutlined, TwitterOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { DiscordOutlined } from './Icons'

export const AuthenticationManager = ({ children }: { children?: ReactNode }) => {
  const client = useContext(ClientContext)

  return <>
    <Modal
      visible={!client.currentInfo.user}
      closable={false}
      maskClosable={false}
      title={<>
        <HeartOutlined />
        {' '}
        <FormattedMessage id='auth.title' />
      </>}
      footer={null}>

      <p>
        <span>nhitomi — <FormattedMessage id='auth.tagline' /></span>
        <br />
        <Typography.Text type='secondary'><FormattedMessage id='auth.description' /></Typography.Text>
      </p>

      <Button type='primary' style={{ background: '#7289DA', borderColor: '#7289DA' }} shape='round' icon={<DiscordOutlined />}>
        <span><FormattedMessage id='auth.connect.discord' /></span>
      </Button>
    </Modal>

    {client.currentInfo.user && children}
  </>
}
