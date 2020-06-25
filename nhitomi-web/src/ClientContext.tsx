import { createContext, ReactNode, useState, useMemo, useContext, useLayoutEffect } from 'react'
import { Client } from './Client'
import { useAsync } from 'react-use'
import React from 'react'
import { ProgressContext } from './Progress'
import { LocaleContext } from './LocaleProvider'

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

  useLayoutEffect(() => {
    const resetNow = () => setReset(reset + 1)

    client.config.on('token', resetNow)
    client.config.on('baseUrl', resetNow)
    return () => {
      client.config.off('token', resetNow)
      client.config.off('baseUrl', resetNow)
    }
  }, [client, reset])

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

  useLayoutEffect(() => {
    if (initialized)
      stop()
    else
      start()
  }, [!!initialized]) // eslint-disable-line

  if (initialized instanceof Error)
    return <code>{initialized.stack}</code>

  return <ClientContext.Provider key={reset} value={client}>
    {initialized && children}
  </ClientContext.Provider>
}
