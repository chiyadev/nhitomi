import { InfoApi, BookApi, UserApi, CollectionApi } from 'nhitomi-api'
import config from 'config'

/** Represents an nhitomi API client. */
export class ApiClient {
  readonly info: InfoApi
  readonly user: UserApi
  readonly book: BookApi
  readonly collection: CollectionApi

  constructor() {
    this.info = new InfoApi()
    this.user = new UserApi()
    this.book = new BookApi()
    this.collection = new CollectionApi()
  }

  /** URL to make API requests to. */
  baseUrl = ''

  /** URL to use to format links. */
  publicUrl = ''

  initialize(token: string): void {
    for (const part of [this.info, this.user, this.book, this.collection]) {
      part.basePath = this.baseUrl = config.get<string>('api.baseUrl') || part.basePath
      part.accessToken = token
    }

    this.publicUrl = config.get<string>('api.publicUrl')
  }

  /** Formats a link using publicUrl. */
  getLink(route: string): string {
    if (route.startsWith('/')) route = route.substring(1)

    if (!route)
      return this.publicUrl

    return `${this.publicUrl}/${route}`
  }

  /** Contains API client pooling. */
  static pool = {
    instances: [] as ApiClient[],

    rent(token: string): ApiClient {
      let client = this.instances.pop()

      if (!client) {
        // pooling is used because api client instances are expensive to construct
        client = new ApiClient()
      }

      client.initialize(token)
      return client
    },

    return(client: ApiClient): void {
      this.instances.push(client)

      // clear token for safety
      client.initialize('')
    }
  }
}

/** Global nhitomi API client authenticated as bot user. */
export const Api = new ApiClient()
Api.initialize(config.get<string>('api.token'))
