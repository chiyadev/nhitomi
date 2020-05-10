import { InfoApi, BookApi, UserApi, CollectionApi, InternalApi } from 'nhitomi-api'
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

  /** URL to make API requests to. */
  get baseUrl(): string { return this.info.basePath }

  /** URL to use to format links. */
  get publicUrl(): string { return config.get<string>('api.publicUrl') }

  /** Formats a link using publicUrl. */
  getLink(route: string): string {
    if (route.startsWith('/')) route = route.substring(1)

    if (!route)
      return this.publicUrl

    return `${this.publicUrl}/${route}`
  }

  /** Destroys this API client, making it unusable. */
  destroy(): void {
    if (this._core) {
      returnCore(this._core)
    }

    this._core = undefined
  }
}

/** Global nhitomi API client authenticated as bot user. */
export const Api = new ApiClient(config.get<string>('api.token'))
