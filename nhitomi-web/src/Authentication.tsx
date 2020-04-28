import React, { useContext, ReactNode, useMemo } from 'react'
import { Modal, Typography, Button } from 'antd'
import { ClientContext } from './ClientContext'
import { HeartOutlined } from '@ant-design/icons'
import { FormattedMessage } from 'react-intl'
import { DiscordOutlined } from './Icons'
import { useAsync } from 'react-use'
import { NotificationContext } from './NotificationContext'
import { useHistory } from 'react-router-dom'
import qs from 'qs'
import { AuthenticateResponse } from './client'
import { ProgressContext } from './Progress'

function createOAuthState() {
  const x = Math.random().toString(36).substring(2)
  localStorage.setItem('state', x)
  return x
}

function getOAuthState() {
  return localStorage.getItem('state') || undefined
}

export const AuthenticationManager = ({ children }: { children?: ReactNode }) => {
  const client = useContext(ClientContext)
  const state = useMemo(createOAuthState, [])

  return <>
    <Modal
      visible={!client.currentInfo.authenticated}
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

      <a href={`${client.currentInfo.discordOAuthUrl}&state=${state}`}>
        <Button type='primary' style={{ background: '#7289DA', borderColor: '#7289DA' }} icon={<DiscordOutlined />}>
          <span><FormattedMessage id='auth.connect.discord' /></span>
        </Button>
      </a>
    </Modal>

    {client.currentInfo.authenticated && children}
  </>
}

export const AuthenticationRoute = ({ service }: { service: 'discord' }) => {
  const client = useContext(ClientContext)
  const { notification } = useContext(NotificationContext)
  const { start, stop } = useContext(ProgressContext)

  const { location: { search }, replace } = useHistory()
  const { code, state } = qs.parse(search.slice(1))

  useAsync(async () => {
    start()

    try {
      const savedState = getOAuthState()

      let response: AuthenticateResponse | undefined

      switch (code && (!savedState || savedState === state) && service) {
        case 'discord':
          response = await client.user.authenticateUserDiscord({ authenticateDiscordRequest: { code } })
          break
      }

      if (response)
        client.config.token = response.token

      replace('/')
    }
    catch (e) {
      notification.error(e, <FormattedMessage id='auth.failed' />)
    }
    finally {
      stop()
    }
  }, [])

  return null
}
