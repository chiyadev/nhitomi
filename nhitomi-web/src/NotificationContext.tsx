import { MessageApi } from 'antd/lib/message'
import { NotificationInstance } from 'antd/lib/notification'
import { createContext, ReactNode, useMemo } from 'react'
import React from 'react'
import { ValidationError } from './client/validationError'

/** Exposes the notification and message API via a context. */
export const NotificationContext = createContext<{
  notification: NotificationApi
  alert: AlertApi
}>(undefined as any)

export type NotificationApi = {
  success: (title: ReactNode, description: ReactNode) => void
  warning: (title: ReactNode, description: ReactNode) => void,
  error: (error: Error, title?: ReactNode) => void
}

export type AlertApi = {
  success: (message: ReactNode) => void
  info: (message: ReactNode) => void
  warning: (message: ReactNode) => void
}

function wrapNotifDescription(node: ReactNode, mode?: 'chunk' | 'formatted') {
  switch (mode) {
    case 'formatted':
      return <pre
        style={{
          overflow: 'hidden',
          whiteSpace: 'normal',
          margin: 0,
          padding: 0
        }}
        children={node} />

    case 'chunk':
      return <pre children={node} />

    default:
      return <p children={node} />
  }
}

// wraps antd notification and message apis for convenience and styling consistence
export const NotificationProvider = ({ notif, alert, children }: { notif: NotificationInstance, alert: MessageApi, children?: ReactNode }) =>
  <NotificationContext.Provider
    value={useMemo(() => ({
      notification: {
        success: (title, description) => {
          notif.success({
            message: title,
            description: wrapNotifDescription(description)
          })
        },
        warning: (title, description) => {
          notif.warning({
            message: title,
            description: wrapNotifDescription(description)
          })
        },
        error: (error, title) => {
          if (error instanceof ValidationError)
            notif.error({
              message: title || error.message,
              description: wrapNotifDescription(<small><ul style={{ paddingLeft: '2em' }}>{error.list.map((p, i) => <li key={i}><strong>{p.field}</strong>: <code>{p.messages.join(' ')}</code><br /></li>)}</ul></small>, 'formatted')
            })

          else
            notif.error({
              message: title || error.message,
              description: wrapNotifDescription(<small><code>{error.stack || 'Unknown reason.'}</code></small>, 'chunk')
            })
        }
      },
      alert
    }), [
      notif,
      alert
    ])}
    children={children} />
