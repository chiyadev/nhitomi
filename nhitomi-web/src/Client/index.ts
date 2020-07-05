import { BASE_PATH, RequestContext, ResponseContext, GetInfoResponse, ConfigurationParameters, ValidationProblemArrayResult, InfoApi, BookApi, UserApi, Configuration, GetInfoAuthenticatedResponse, CollectionApi } from 'nhitomi-api'
import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'
import { ValidationError } from './validationError'
import { ConfigManager } from './config'
import { ImageWorker } from './image'

export * from 'nhitomi-api'

/** Implements nhitomi API client. */
export class Client extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, {
  request: (request: RequestContext) => void
  response: (response: ResponseContext) => void
}>) {
  private readonly httpConfig: ConfigurationParameters = {
    middleware: [{
      pre: async context => {
        this.emit('request', context)
      },
      post: async context => {
        const { response } = context

        this.emit('response', context)

        // success
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

        // other errors
        const error = Error((await response.json())?.message || response.statusText)

        console.error(error)

        throw error
      }
    }]
  }

  /** User API. */
  user: UserApi

  /** Info API */
  info: InfoApi

  /** Book API. */
  book: BookApi

  /** Collection API. */
  collection: CollectionApi

  /** Configuration manager. */
  config: ConfigManager

  /** Image worker. */
  image: ImageWorker

  /** Contains client and API information. */
  currentInfo!:
    GetInfoResponse & { authenticated: false } |
    GetInfoAuthenticatedResponse & { authenticated: true }

  /**
   * Creates a new nhitomi client.
   */
  constructor() {
    super()

    // this.on('httpRequest', ({ init, url }) => console.log('sending http', init.method, url, init))
    // this.on('httpResponse', ({ init, url, response }) => console.log('received http', init.method, url, response))

    this.user = new UserApi(new Configuration(this.httpConfig))
    this.info = new InfoApi(new Configuration(this.httpConfig))
    this.book = new BookApi(new Configuration(this.httpConfig))
    this.collection = new CollectionApi(new Configuration(this.httpConfig))

    this.config = new ConfigManager(this)
    this.image = new ImageWorker(this)
  }

  /** Initializes this client. */
  async initialize() {
    const url = new URL(BASE_PATH)

    // use current domain if base path is localhost
    if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
      url.host = window.location.host
      url.protocol = window.location.protocol
    }

    // ensure token doesn't change until reinitialized
    this.httpConfig.accessToken = this.config.token
    this.httpConfig.basePath = this.config.baseUrl || url.href

    console.log('api base path', this.httpConfig.basePath)

    if (this.httpConfig.accessToken)
      this.currentInfo = {
        ...await this.info.getInfoAuthenticated(),
        authenticated: true
      }

    else
      this.currentInfo = {
        ...await this.info.getInfo(),
        authenticated: false
      }
  }
}
