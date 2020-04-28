import { createContext, ReactNode, useState, useMemo, useEffect, useContext } from 'react'
import { Client } from './Client'
import { useAsync } from 'react-use'
import React from 'react'
import { Modal, Divider } from 'antd'
import { ApiOutlined } from '@ant-design/icons'
import { ProgressContext } from './Progress'
import { LocaleContext } from './LocaleProvider'
import { FormattedMessage } from 'react-intl'

/** API client context. */
export const ClientContext = createContext<Client>(undefined as any)

export const ClientProvider = ({ children }: { children?: ReactNode }) => {
  const [initialized, setInitialized] = useState<boolean | Error>(false)
  const { start, stop } = useContext(ProgressContext)
  const { setLocale } = useContext(LocaleContext)

  // create client
  const client = useMemo(() => new Client(), [])

  // reset app completely when token changes
  const [reset, setReset] = useState(0)

  useEffect(() => {
    const handle = () => setReset(reset + 1)

    client.config.on('token', handle)

    return () => { client.config.off('token', handle) }
  }, [client, reset])

  // initialize client
  useAsync(async () => {
    setInitialized(false)

    try {
      await client.initialize()

      setInitialized(true)

      if (client.currentInfo.authenticated)
        setLocale(client.currentInfo.user.language)
    }
    catch (e) {
      setInitialized(e)
    }
  }, [reset])

  const initializing = !initialized || initialized instanceof Error

  useEffect(() => {
    if (initializing)
      start()
    else
      stop()
  }, [initializing]) // eslint-disable-line

  return <ClientContext.Provider key={reset} value={client}>
    {initialized && !(initialized instanceof Error) && children}

    {initialized instanceof Error && <ErrorDisplay error={initialized} />}
  </ClientContext.Provider>
}

const ErrorDisplay = ({ error }: { error: Error }) => {
  return <Modal
    visible
    closable={false}
    footer={null}
    title={<>
      <ApiOutlined />
      {' '}
      <FormattedMessage id='api.connectionFailed.title' />
    </>}>

    <p><FormattedMessage id='api.connectionFailed.description' /></p>
    <br />
    <Divider />
    <p><strong>{error.name}</strong>: {error.message || '<unknown reason>'}</p>
    <p><code>{error.stack}</code></p>
  </Modal>
}
