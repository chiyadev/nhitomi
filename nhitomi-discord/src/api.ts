import { InfoApi, BookApi, UserApi, CollectionApi, InternalApi, GetInfoAuthenticatedResponse } from 'nhitomi-api'
import config from 'config'

type ApiClientCore = {
  readonly info: InfoApi
  readonly user: UserApi
  readonly book: BookApi
  readonly collection: CollectionApi
  readonly internal: InternalApi
}

const cores: ApiClientCore[] = []

function rentCore(token: string): ApiClientCore {
  let core = cores.pop()

  if (!core)
    core = {
      info: new InfoApi(),
      user: new UserApi(),
      book: new BookApi(),
      collection: new CollectionApi(),
      internal: new InternalApi()
    }

  for (const key in core) {
    const api = (core as Record<string, (typeof core)[keyof typeof core]>)[key]

    api.basePath = config.get<string>('api.baseUrl') || api.basePath
    api.accessToken = token
  }

  return core
}

function returnCore(core: ApiClientCore): void {
  cores.push(core)
}

/** Represents an nhitomi API client. */
export class ApiClient implements ApiClientCore {
  _core?: ApiClientCore

  get core(): ApiClientCore {
    const core = this._core

    if (core) return core
    throw Error('API client was destroyed.')
  }

  get info(): InfoApi { return this.core.info }
  get user(): UserApi { return this.core.user }
  get book(): BookApi { return this.core.book }
  get collection(): CollectionApi { return this.core.collection }
  get internal(): InternalApi { return this.core.internal }

  constructor(token: string) {
    this._core = rentCore(token)
  }

  /** Destroys this API client, making it unusable. */
  destroy(): void {
    if (this._core) {
      returnCore(this._core)
    }

    this._core = undefined
  }
}

class BotApiClient extends ApiClient {
  constructor() { super(config.get<string>('api.token')) }

  /** Bot API information. */
  currentInfo!: GetInfoAuthenticatedResponse

  /** URL to make API requests to. */
  get baseUrl(): string { return this.info.basePath }

  /** URL to use to format links. */
  get publicUrl(): string { return this.currentInfo.publicUrl }

  /** Formats a link to an API route using publicUrl. */
  getApiLink(path: string): string {
    if (path.startsWith('/')) path = path.substring(1)

    return this.getWebLink(`api/v1/${path}`)
  }

  /** Formats a link to a frontend route using publicUrl. */
  getWebLink(path: string): string {
    if (path.startsWith('/')) path = path.substring(1)
    if (path.endsWith('/')) path = path.slice(0, -1)

    if (!path)
      return this.publicUrl

    return `${this.publicUrl}/${path}`
  }

  async initialize(): Promise<void> {
    const { body: info } = await this.info.getInfoAuthenticated()

    const refresh = !!this.currentInfo
    this.currentInfo = info

    if (!refresh)
      console.log('api initialized', this.currentInfo)
  }

  destroy(): void {
    // prevent bot client destruction
  }
}

/** Global nhitomi API client authenticated as bot user. */
export const Api = new BotApiClient()

// refresh info every minute
setInterval(() => Api.initialize(), 60 * 1000)
