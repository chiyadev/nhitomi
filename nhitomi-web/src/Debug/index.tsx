import React from 'react'
import { useNotify, useAlert } from '../NotificationManager'
import { FlatButton } from '../Components/FlatButton'
import { Container } from '../Components/Container'
import { useTabTitle } from '../TitleSetter'

export const Debug = () => {
  useTabTitle('Debug helper')

  return (
    <Container className='text-sm space-y-4'>
      <Notifications />
      <Alerts />
    </Container>
  )
}

const Notifications = () => {
  const { notify, notifyError } = useNotify()

  return (
    <div>
      <div>Notifications</div>
      <div className='space-x-1'>
        <FlatButton onClick={() => notify('info', 'message', 'description')}>Notify info</FlatButton>
        <FlatButton onClick={() => notify('success', 'message', 'description')}>Notify success</FlatButton>
        <FlatButton onClick={() => notify('warning', 'message', 'description')}>Notify warning</FlatButton>
        <FlatButton onClick={() => notifyError(Error('notify error'))}>Notify error</FlatButton>
      </div>
    </div>
  )
}

const Alerts = () => {
  const { alert } = useAlert()

  return (
    <div>
      <div>Alerts</div>
      <div className='space-x-1'>
        <FlatButton onClick={() => alert('message')}>Alert</FlatButton>
        <FlatButton onClick={() => alert('message', 'info')}>Alert info</FlatButton>
        <FlatButton onClick={() => alert('message', 'success')}>Alert success</FlatButton>
        <FlatButton onClick={() => alert('message', 'warning')}>Alert warning</FlatButton>
        <FlatButton onClick={() => alert('message', 'error')}>Alert error</FlatButton>
      </div>
    </div>
  )
}
