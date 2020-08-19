import React, { ReactNode, createContext, useContext, useMemo, useState } from 'react'
import { ToastProvider, useToasts, ToastProps, ToastContainerProps, AppearanceTypes } from 'react-toast-notifications'
import { CloseOutlined, CheckCircleTwoTone, InfoCircleTwoTone, CloseCircleTwoTone, WarningTwoTone } from '@ant-design/icons'
import { cx, css } from 'emotion'
import { useSpring, animated } from 'react-spring'
import { useLayout } from './LayoutManager'
import { ValidationError } from './ClientManager'
import { ColorHue, getColor } from './theme'

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

const ToastContainer = ({ children, placement, hasToasts }: ToastContainerProps) => {
  const { screen } = useLayout()

  return (
    <animated.div
      className={(
        cx('w-screen fixed p-4 z-50 text-center', { 'max-w-md': screen === 'lg' }, { 'pointer-events-none': !hasToasts }, placement
          .replace('-', ' ')
          .replace('top', 'top-0')
          .replace('bottom', 'bottom-0')
          .replace('left', 'left-0')
          .replace('right', 'right-0')
          .replace('center', css`transform: translateX(-50%); left: 50%;`))
      )}
      children={children} />
  )
}

const NotifyToast = ({ children, onMouseEnter, onMouseLeave, transitionState, transitionDuration, onDismiss }: ToastProps) => {
  const style = useSpring({
    config: { duration: transitionDuration },
    opacity: transitionState === 'entered' ? 1 : 0,
    transform: transitionState === 'entered' ? 'translateX(0)' : 'translateX(5px)'
  })

  const [closeHover, setCloseHover] = useState(false)
  const closeStyle = useSpring({
    transform: closeHover ? 'scale(1.1)' : 'scale(1)'
  })

  return (
    <animated.div
      style={style}
      className='relative w-full rounded overflow-hidden bg-white text-left text-black shadow-lg p-3 mb-3'
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}>

      {children}

      <animated.span
        style={closeStyle}
        className='absolute top-0 right-0 p-3 text-gray-darker text-sm'>

        <CloseOutlined
          className='cursor-pointer'
          onClick={() => onDismiss()}
          onMouseEnter={() => setCloseHover(true)}
          onMouseLeave={() => setCloseHover(false)} />
      </animated.span>
    </animated.div>
  )
}

function convertTypeToIcon(type: AppearanceTypes) {
  let Icon: typeof CloseCircleTwoTone
  let color: ColorHue

  switch (type) {
    case 'error': Icon = CloseCircleTwoTone; color = 'red'; break
    case 'info': Icon = InfoCircleTwoTone; color = 'blue'; break
    case 'success': Icon = CheckCircleTwoTone; color = 'green'; break
    case 'warning': Icon = WarningTwoTone; color = 'orange'; break
  }

  return <Icon className='text-lg w-6' twoToneColor={getColor(color).hex} />
}

const NotifyToastContent = ({ type, title, description }: { type: AppearanceTypes, title?: ReactNode, description?: ReactNode }) => {
  return <>
    <div className='mb-3'>
      {convertTypeToIcon(type)}
      {' '}
      {title}
    </div>

    <div className='text-xs overflow-auto'>
      {description}
    </div>
  </>
}

const NotifyManager = ({ children }: { children?: ReactNode }) => {
  const { addToast } = useToasts()

  return (
    <NotifyContext.Provider children={children} value={useMemo(() => ({
      notify: (type, title, description) => {
        addToast(<NotifyToastContent type={type} title={title} description={description} />)
      },
      notifyError: (error, title) => {
        if (!(error instanceof Error))
          error = Error((error as any)?.message || 'Unknown error.')

        if (error instanceof ValidationError) {
          addToast(
            <NotifyToastContent
              type='error'
              title={title || error.message}
              description={(
                <ul className='list-disc list-inside'>
                  {error.list.map(problem => (
                    <li>
                      <code>{problem.field} </code>
                      <span>{problem.messages.join(' ')}</span>
                    </li>
                  ))}
                </ul>
              )} />
          )
        }
        else {
          addToast(
            <NotifyToastContent
              type='error'
              title={title || error.message}
              description={(
                <code>{error.stack}</code>
              )} />
          )
        }
      }
    }), [addToast])} />
  )
}

const AlertToast = ({ children, onMouseEnter, onMouseLeave, transitionState, transitionDuration }: ToastProps) => {
  const style = useSpring({
    config: { duration: transitionDuration },
    opacity: transitionState === 'entered' ? 1 : 0,
    transform: transitionState === 'entered' ? 'translateY(0)' : 'translateY(-1em)'
  })

  return (
    <animated.div
      style={style}
      className='table mx-auto rounded overflow-hidden bg-gray-darkest bg-blur text-white text-sm shadow-lg p-3'
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}>

      {children}
    </animated.div>
  )
}

const AlertManager = ({ children }: { children?: ReactNode }) => {
  const { addToast, removeAllToasts } = useToasts()

  return (
    <AlertContext.Provider children={children} value={useMemo(() => ({
      alert: (message, type) => {
        let content = <span>{message}</span>

        if (type)
          content = <span>{convertTypeToIcon(type)} {content}</span>

        removeAllToasts()
        addToast(content)
      }
    }), [addToast, removeAllToasts])} />
  )
}
