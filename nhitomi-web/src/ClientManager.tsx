import React, { ReactNode, useMemo, createContext, useState, useContext, Dispatch, useLayoutEffect, useRef } from 'react'
import { ConfigurationParameters, ValidationProblemArrayResult, ValidationProblem, UserApi, InfoApi, BookApi, CollectionApi, Configuration, GetInfoResponse, GetInfoAuthenticatedResponse, BASE_PATH, User, UserPermissions, Collection, InternalApi } from 'nhitomi-api'
import { CustomError } from 'ts-custom-error'
import { useAsync, useInterval } from 'react-use'
import { useProgress } from './ProgressManager'
import { ConfigSource, useConfigManager, useConfig } from './ConfigManager'
import { Container } from './Components/Container'
import { FlatButton } from './Components/FlatButton'
import { ReloadOutlined, ClearOutlined } from '@ant-design/icons'
import { getColor } from './theme'
import { FilledButton } from './Components/FilledButton'
import * as ga from 'react-ga'
import { JSONex } from './jsonEx'

const gaApiIgnorePaths = [
  /books\/.*\/contents\/.*\/pages\/.*/g
]

export class Client {
  private readonly httpConfig: ConfigurationParameters = {
    middleware: [{
      pre: async context => {
        (context.init as any).startTime = performance.now()
      },
      post: async context => {
        if (gaApiIgnorePaths.findIndex(g => context.url.match(g)) === -1) {
          const time = performance.now() - (context.init as any).startTime

          ga.timing({
            variable: context.url.replace(this.httpConfig.basePath || '', ''),
            category: 'api',
            value: time
          })
        }

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
  readonly internal: InternalApi

  constructor(private readonly config: ConfigSource) {
    this.user = new UserApi(new Configuration(this.httpConfig))
    this.info = new InfoApi(new Configuration(this.httpConfig))
    this.book = new BookApi(new Configuration(this.httpConfig))
    this.collection = new CollectionApi(new Configuration(this.httpConfig))
    this.internal = new InternalApi(new Configuration(this.httpConfig))

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

const cacheKey = 'info_cached'
type CachedClientInfo = {
  time: number
  value: ClientInfo
}

/** Cached client info allows the site to load faster. */
function getCachedInfo(): ClientInfo | undefined {
  try {
    const cached: CachedClientInfo = JSONex.parse(localStorage.getItem(cacheKey) || '')
    const now = Date.now()

    if (now - cached.time < 1000 * 60 * 60) // cache valid for a day
      return cached.value
  }
  catch {
    // ignored
  }
}

function setCachedInfo(value?: ClientInfo) {
  if (value) {
    const cached: CachedClientInfo = {
      time: Date.now(),
      value
    }

    localStorage.setItem(cacheKey, JSON.stringify(cached))
  }
  else {
    localStorage.removeItem(cacheKey)
  }
}

export const ClientManager = ({ children }: { children?: ReactNode }) => {
  const config = useConfigManager()

  const client = useMemo(() => new Client(config), [config])
  const [info, setInfo] = useState<ClientInfo | Error | undefined>(getCachedInfo)
  const { begin, end } = useProgress()

  useLayoutEffect(() => {
    if (info && !(info instanceof Error))
      setCachedInfo(info)
    else
      setCachedInfo(undefined)
  }, [info])

  const lastGtag = useRef<string>()

  useLayoutEffect(() => {
    if (info && !(info instanceof Error)) {
      if (info.gTag && lastGtag.current !== info.gTag) {
        ga.initialize(info.gTag, {
          debug: process.env.NODE_ENV === 'development',
          titleCase: false,
          gaOptions: {
            userId: info.authenticated ? info.user.id : undefined
          }
        })
      }

      lastGtag.current = info.gTag
    }
  }, [info])

  const lastVersion = useRef<string>()

  useLayoutEffect(() => {
    if (info && !(info instanceof Error)) {
      // reload if version changes
      if (lastVersion.current && lastVersion.current !== info.version.hash)
        window.location.reload()

      lastVersion.current = info.version.hash
    }
  }, [info])

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

      ga.exception({
        description: e.message,
        fatal: true
      })
    }
    finally {
      end()
    }
  }, [])

  // periodically refresh info
  useInterval(async () => {
    try {
      setInfo(await client.getInfo())
    }
    catch (e) {
      console.warn('could not refresh info', e)
    }
  }, 1000 * 60)

  const [, setToken] = useConfig('token')
  const [, setBaseUrl] = useConfig('baseUrl')

  if (!info)
    return null

  if (info instanceof Error) {
    return (
      <Container className='text-sm p-4'>
        <div className='mb-2'>nhitomi could not contact the API server. Please try again later.</div>
        <code>{info.stack}</code>
        <div className='mt-4 space-x-1'>
          <FilledButton icon={<ReloadOutlined />} onClick={() => window.location.reload()} color={getColor('red')}>Retry</FilledButton>
          <FlatButton icon={<ClearOutlined />} onClick={() => { setToken(undefined); setBaseUrl(undefined); window.location.reload() }}>Reset</FlatButton>
        </div>
      </Container>
    )
  }

  return (
    <Loaded client={client} info={info} setInfo={setInfo} children={children} />
  )
}

const Loaded = ({ client, info, setInfo, children }: { client: Client, info: ClientInfo, setInfo: Dispatch<ClientInfo>, children?: ReactNode }) => (
  <ClientContext.Provider
    children={children}
    value={useMemo(() => ({
      client,
      permissions: new PermissionHelper(info?.authenticated ? info.user : undefined),
      info,
      setInfo,
      fetchInfo: async () => {
        const info = await client.getInfo()
        setInfo(info)
        return info
      }
    }), [client, info, setInfo])} />
)

export class PermissionHelper {
  constructor(readonly user?: User) { }

  get isAdministrator() {
    return this.permissions.indexOf(UserPermissions.Administrator) !== -1
  }

  get permissions() {
    return this.user?.permissions || []
  }

  isSelf(user: User) {
    return this.user?.id === user.id
  }

  hasPermissions(...permissions: UserPermissions[]) {
    if (this.isAdministrator)
      return true

    for (const permission of permissions) {
      if (this.permissions.indexOf(permission) === -1)
        return false
    }

    return true
  }

  hasAnyPermission(...permissions: UserPermissions[]) {
    if (this.isAdministrator)
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
