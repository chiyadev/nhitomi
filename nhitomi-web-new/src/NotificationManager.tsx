import React, { ReactNode, createContext, useContext, useMemo } from 'react'
import { ToastProvider, useToasts, ToastProps, ToastContainerProps, AppearanceTypes } from 'react-toast-notifications'
import { CloseOutlined, CheckCircleTwoTone, InfoCircleTwoTone, CloseCircleTwoTone, WarningTwoTone } from '@ant-design/icons'
import { colors } from './theme.json'
import { cx, css } from 'emotion'

export const NotifyContext = createContext<{
  notify: (type: AppearanceTypes, title: ReactNode, description: ReactNode) => void
  notifyError: (error: Error, title?: ReactNode) => void
}>(undefined as any)

export const AlertContext = createContext<{
  alert: (message: ReactNode, type?: AppearanceTypes) => void
}>(undefined as any)

export function useNotify() {
  return useContext(NotifyContext)
}

export function useAlert() {
  return useContext(AlertContext)
}

export const NotificationManager = ({ children }: { children?: ReactNode }) => {
  return (
    <ToastProvider components={{ Toast: NotifyToast, ToastContainer: ToastContainer }} placement='top-right' autoDismiss autoDismissTimeout={15000}>
      <NotifyManager>
        <ToastProvider components={{ Toast: AlertToast, ToastContainer: ToastContainer }} placement='top-center' autoDismiss autoDismissTimeout={5000}>
          <AlertManager>
            {children}
          </AlertManager>
        </ToastProvider>
      </NotifyManager>
    </ToastProvider>
  )
}

const ToastContainer = ({ children, placement }: ToastContainerProps) => (
  <div
    className={(
      cx('w-screen lg:max-w-md fixed p-4 z-10 text-center', placement
        .replace('-', ' ')
        .replace('top', 'top-0')
        .replace('bottom', 'bottom-0')
        .replace('left', 'left-0')
        .replace('right', 'right-0')
        .replace('center', css`transform: translateX(-50%); left: 50%;`))
    )}
    children={children} />
)

const NotifyToast = ({ children, onMouseEnter, onMouseLeave, transitionState, transitionDuration, onDismiss }: ToastProps) => {
  const opacity = transitionState === 'entered' ? 'opacity-100' : 'opacity-0'

  return (
    <div
      className={cx('relative w-full rounded bg-white text-left text-black shadow-lg p-3 mb-3 transition', opacity, css`transition-duration: ${transitionDuration}ms;`)}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}>

      {children}

      <CloseOutlined className='absolute top-0 right-0 p-3 text-gray-800 text-sm cursor-pointer' onClick={() => onDismiss()} />
    </div>
  )
}

function convertTypeToIcon(type: AppearanceTypes) {
  let Icon: typeof CloseCircleTwoTone
  let color: string

  switch (type) {
    case 'error': Icon = CloseCircleTwoTone; color = colors.red[500]; break
    case 'info': Icon = InfoCircleTwoTone; color = colors.blue[500]; break
    case 'success': Icon = CheckCircleTwoTone; color = colors.green[500]; break
    case 'warning': Icon = WarningTwoTone; color = colors.orange[500]; break
  }

  return <Icon className='align-middle text-lg w-6' twoToneColor={color} />
}

const NotifyToastContent = ({ type, title, description }: { type: AppearanceTypes, title?: ReactNode, description?: ReactNode }) => {
  return <>
    <p className='mb-3'>
      {convertTypeToIcon(type)}
      {' '}
      {title}
    </p>

    <p className='text-xs overflow-auto'>
      {description}
    </p>
  </>
}

const NotifyManager = ({ children }: { children?: ReactNode }) => {
  const { addToast } = useToasts()

  return (
    <NotifyContext.Provider children={children} value={useMemo(() => ({
      notify: (type, title, description) => addToast(<NotifyToastContent type={type} title={title} description={description} />),
      notifyError: (error, title) => addToast(<NotifyToastContent type='error' title={title || error.message} description={<code>{error.stack}</code>} />)
    }), [addToast])} />
  )
}

const AlertToast = ({ children, onMouseEnter, onMouseLeave, transitionState, transitionDuration }: ToastProps) => {
  const opacity = transitionState === 'entered' ? 'opacity-100' : 'opacity-0'

  return (
    <div
      className={cx('inline-block rounded bg-black text-white text-sm shadow-lg p-3 transition', opacity, css`transition-duration: ${transitionDuration}ms;`)}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}>

      {children}
    </div>
  )
}

const AlertManager = ({ children }: { children?: ReactNode }) => {
  const { addToast, removeAllToasts } = useToasts()

  return (
    <AlertContext.Provider children={children} value={useMemo(() => ({
      alert: (message, type) => { removeAllToasts(); addToast(<span>{type && convertTypeToIcon(type)}{' '}{message}</span>) }
    }), [addToast, removeAllToasts])} />
  )
}
