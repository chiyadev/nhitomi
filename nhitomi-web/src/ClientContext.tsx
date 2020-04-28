import { createContext, ReactNode, useState, useMemo, useEffect, useContext } from 'react'
import { Client } from './client'
import { useAsync } from 'react-use'
import React from 'react'
import { Modal, Divider } from 'antd'
import { ApiOutlined } from '@ant-design/icons'
import { ProgressContext } from './Progress'
import { LocaleContext } from './LocaleProvider'

/** API client context. */
export const ClientContext = createContext<Client>(undefined as any)

export const ClientProvider = ({ children }: { children?: ReactNode }) => {
  const { start, stop } = useContext(ProgressContext)
  const [initialized, setInitialized] = useState<boolean | Error>(false)
  const { setLocale } = useContext(LocaleContext)

  // create client
  const client = useMemo(() => new Client(), [])

  // initialize client
  useAsync(async () => {
    try {
      await client.initialize()

      setInitialized(true)

      if (client.currentInfo.user)
        setLocale(client.currentInfo.user.language)
    }
    catch (e) {
      setInitialized(e)
    }
  }, [])

  const initializing = !initialized || initialized instanceof Error

  useEffect(() => {
    if (initializing)
      start()
    else
      stop()
  }, [initializing]) // eslint-disable-line

  return <ClientContext.Provider value={client}>
    {initialized && !(initialized instanceof Error) && children}

    {initialized instanceof Error && <ErrorDisplay error={initialized} />}
  </ClientContext.Provider>
}

const ErrorDisplay = ({ error }: { error: Error }) => {
  const { currentInfo } = useContext(ClientContext)

  return <Modal
    visible
    closable={false}
    footer={null}
    title={<>
      <ApiOutlined />
      <span> {currentInfo ? 'Connection Lost' : 'Connection Failed'}</span>
    </>}>

    <p>Could not connect to nhitomi. We will automatically try again.</p>
    <br />
    <Divider />
    <p><strong>{error.name}</strong>: {error.message || '<unknown reason>'}</p>
    <p><code>{error.stack}</code></p>
  </Modal>
}
