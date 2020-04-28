import { MessageApi, ArgsProps } from 'antd/lib/message'
import { NotificationInstance } from 'antd/lib/notification'
import { createContext, ReactNode, useMemo, useContext, useCallback, useEffect } from 'react'
import React from 'react'
import { ValidationError } from './client/validationError'
import { FormattedMessage, useIntl, IntlProvider } from 'react-intl'
import { LocaleContext } from './LocaleProvider'

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

/** Note: You can only use localization contexts inside message content component. */
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
export const NotificationProvider = ({ notif, alert, children }: { notif: NotificationInstance, alert: MessageApi, children?: ReactNode }) => {
  const intl = useIntl()
  const locale = useContext(LocaleContext)

  // wrap alert with localization context
  const openAlert = useCallback((args: ArgsProps) => alert.open({
    ...args,
    content:
      <React.StrictMode>
        <LocaleContext.Provider value={locale}>
          <IntlProvider {...intl}>
            {args.content}
          </IntlProvider>
        </LocaleContext.Provider>
      </React.StrictMode>
  }), [
    alert,
    locale,
    intl
  ])

  useEffect(() => {
    alert.config({
      maxCount: 1
    })
  }, [alert])

  return <NotificationContext.Provider
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
              description: wrapNotifDescription(<small>
                <ul style={{ paddingLeft: '2em' }}>
                  {error.list.map((p, i) =>
                    <li key={i}>
                      <strong>{p.field}</strong>
                      {': '}
                      <code>{p.messages.join(' ')}</code>
                      <br />
                    </li>)}
                </ul>
              </small>, 'formatted')
            })

          else
            notif.error({
              message: title || error.message,
              description: wrapNotifDescription(<small>
                <code>{error.stack || <FormattedMessage id='notification.error.unknownReason' />}</code>
              </small>, 'chunk')
            })
        }
      },
      alert: {
        success: content => openAlert({ type: 'success', duration: null, content }),
        info: content => openAlert({ type: 'info', duration: null, content }),
        warning: content => openAlert({ type: 'warning', duration: null, content })
      }
    }), [
      notif,
      openAlert
    ])}
    children={children} />
}
