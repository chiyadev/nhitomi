import React, { ReactNode, useMemo, createContext, useState, useCallback, useContext } from 'react'
import { ConfigurationParameters, ValidationProblemArrayResult, ValidationProblem, UserApi, InfoApi, BookApi, CollectionApi, Configuration, GetInfoResponse, GetInfoAuthenticatedResponse, BASE_PATH } from 'nhitomi-api'
import { CustomError } from 'ts-custom-error'
import { useAsync } from 'react-use'
import { ProgressContext } from './ProgressManager'
import { ConfigManager } from './ConfigManager'

export class Client {
  private readonly httpConfig: ConfigurationParameters = {
    middleware: [{
      post: async context => {
        const { response } = context

        if (response.ok)
          return

        // authorization failure when we should be authorized
        if (response.status === 401 && this.config.token) {
          this.config.token = undefined // logout
        }

        // validation failure (unprocessable entity)
        else if (response.status === 422) {
          const result: ValidationProblemArrayResult = await response.json()

          throw new ValidationError(result.message, result.value)
        }

        const error = Error((await response.json())?.message || response.statusText)

        console.error(error)

        throw error
      }
    }]
  }

  readonly user: UserApi
  readonly info: InfoApi
  readonly book: BookApi
  readonly collection: CollectionApi

  constructor(readonly config: ConfigManager) {
    this.user = new UserApi(new Configuration(this.httpConfig))
    this.info = new InfoApi(new Configuration(this.httpConfig))
    this.book = new BookApi(new Configuration(this.httpConfig))
    this.collection = new CollectionApi(new Configuration(this.httpConfig))
  }

  async getInfo(): Promise<ClientInfo> {
    const url = new URL(BASE_PATH)

    // use current hostname if default hostname is localhost
    if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
      url.host = window.location.host
      url.protocol = window.location.protocol
    }

    this.httpConfig.accessToken = this.config.token
    this.httpConfig.basePath = this.config.baseUrl || url.href

    console.log('api base path', this.httpConfig.basePath)

    if (this.httpConfig.accessToken)
      return {
        ...await this.info.getInfoAuthenticated(),
        authenticated: true
      }

    else
      return {
        ...await this.info.getInfo(),
        authenticated: false
      }
  }
}

export class ValidationError extends CustomError {
  list: ValidationProblem[]

  constructor(message: string, problems: ValidationProblem[]) {
    super(message)

    this.list = problems
  }

  /** Finds the first validation problem with the given field prefix. */
  find(prefix: string) {
    return this.list.find(p => this.isPrefixed(p, prefix))
  }

  /** Removes all validation problems beginning with the given prefix. */
  remove(prefix: string) {
    this.list = this.list.filter(p => !this.isPrefixed(p, prefix))
  }

  private isPrefixed(problem: ValidationProblem, prefix: string) {
    const field = problem.field.split('.')

    for (let i = 0; i < field.length; i++) {
      const part = field.slice(i).join('.')

      if (part.startsWith(prefix))
        return true
    }

    return false
  }
}

type ClientInfo =
  GetInfoResponse & { authenticated: false } |
  GetInfoAuthenticatedResponse & { authenticated: true }

export const ClientContext = createContext<{
  config: ConfigManager
  client: Client

  info: ClientInfo
  updateInfo: () => Promise<ClientInfo>
}>(undefined as any)

export const ClientManager = ({ children }: { children?: ReactNode }) => {
  const config = useMemo(() => new ConfigManager(), [])
  const client = useMemo(() => new Client(config), [config])
  const [info, setInfo] = useState<ClientInfo | Error>()
  const { begin, end } = useContext(ProgressContext)

  useAsync(async () => {
    begin()

    try {
      setInfo(await client.getInfo())
    }
    catch (e) {
      if (e instanceof Error)
        setInfo(e)
      else
        setInfo(Error(e?.message || 'Unknown error.'))
    }
    finally {
      end()
    }
  }, [])

  const updateInfo = useCallback(async () => { const info = await client.getInfo(); setInfo(info); return info }, [client])
  const context = useMemo(() => ({ config, client, info, updateInfo }), [config, client, info, updateInfo])

  if (!info) {
    return null
  }

  if (info instanceof Error) {
    return <small><code>{info.stack}</code></small>
  }

  return (
    <ClientContext.Provider value={context as any} children={children} />
  )
}
