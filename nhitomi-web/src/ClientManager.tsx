import React, { ReactNode, useMemo, createContext, useState, useCallback, useContext, Dispatch } from 'react'
import { ConfigurationParameters, ValidationProblemArrayResult, ValidationProblem, UserApi, InfoApi, BookApi, CollectionApi, Configuration, GetInfoResponse, GetInfoAuthenticatedResponse, BASE_PATH, User, UserPermissions, Collection } from 'nhitomi-api'
import { CustomError } from 'ts-custom-error'
import { useAsync } from './hooks'
import { useProgress } from './ProgressManager'
import { ConfigSource, useConfigManager } from './ConfigManager'

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

  constructor(private readonly config: ConfigSource) {
    this.user = new UserApi(new Configuration(this.httpConfig))
    this.info = new InfoApi(new Configuration(this.httpConfig))
    this.book = new BookApi(new Configuration(this.httpConfig))
    this.collection = new CollectionApi(new Configuration(this.httpConfig))

    const url = new URL(BASE_PATH)

    // use current hostname if default hostname is localhost
    if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
      url.host = window.location.host
      url.protocol = window.location.protocol
    }

    this.httpConfig.accessToken = () => this.config.token || ''
    this.httpConfig.basePath = this.config.baseUrl || url.href

    console.log('api base path', this.httpConfig.basePath)
  }

  get token() {
    if (typeof this.httpConfig.accessToken === 'function')
      return this.httpConfig.accessToken()
    else
      return this.httpConfig.accessToken
  }

  async getInfo(): Promise<ClientInfo> {
    if (this.token) {
      return {
        ...await this.info.getInfoAuthenticated(),
        authenticated: true
      }
    }
    else {
      return {
        ...await this.info.getInfo(),
        authenticated: false
      }
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

export type ClientInfo =
  GetInfoResponse & { authenticated: false } |
  GetInfoAuthenticatedResponse & { authenticated: true }

const ClientContext = createContext<{
  client: Client
  permissions: PermissionHelper
  info: ClientInfo
  setInfo: Dispatch<ClientInfo>
  fetchInfo: () => Promise<ClientInfo>
}>(undefined as any)

export function useClient() {
  return useContext(ClientContext).client
}

export function useClientInfo() {
  const { permissions, info, setInfo, fetchInfo } = useContext(ClientContext)
  return { permissions, info, setInfo, fetchInfo }
}

export const ClientManager = ({ children }: { children?: ReactNode }) => {
  const config = useConfigManager()

  const client = useMemo(() => new Client(config), [config])
  const [info, setInfo] = useState<ClientInfo | Error>()
  const { begin, end } = useProgress()

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

  if (!info)
    return null

  if (info instanceof Error) {
    return (
      <code className='text-sm'>{info.stack}</code>
    )
  }

  return (
    <Loaded client={client} info={info} setInfo={setInfo} children={children} />
  )
}

const Loaded = ({ client, info, setInfo, children }: { client: Client, info: ClientInfo, setInfo: Dispatch<ClientInfo>, children?: ReactNode }) => {
  const fetchInfo = useCallback(async () => {
    const info = await client.getInfo()
    setInfo(info)
    return info
  }, [client, setInfo])

  return (
    <ClientContext.Provider
      children={children}
      value={useMemo(() => ({
        client,
        permissions: new PermissionHelper(info?.authenticated ? info.user : undefined),
        info,
        setInfo,
        fetchInfo
      }), [client, info, setInfo, fetchInfo])} />
  )
}

export class PermissionHelper {
  constructor(readonly user?: User) { }

  get administrator() {
    return this.permissions.indexOf(UserPermissions.Administrator)
  }

  get permissions() {
    return this.user?.permissions || []
  }

  isSelf(user: User) {
    return this.user?.id === user.id
  }

  hasPermissions(...permissions: UserPermissions[]) {
    if (this.administrator)
      return true

    for (const permission of permissions) {
      if (this.permissions.indexOf(permission) === -1)
        return false
    }

    return true
  }

  hasAnyPermission(...permissions: UserPermissions[]) {
    if (this.administrator)
      return true

    for (const permission of permissions) {
      if (this.permissions.indexOf(permission) !== -1)
        return true
    }

    return false
  }

  canManageCollections(user: User) {
    return this.isSelf(user)
  }

  canManageCollection(collection: Collection) {
    return this.hasPermissions(UserPermissions.ManageUsers) || (this.user && collection.ownerIds.indexOf(this.user.id) !== -1)
  }
}
