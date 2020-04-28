import { RequestContext, ResponseContext, GetInfoResponse, ConfigurationParameters, ValidationProblemArrayResult, InfoApi, BookApi, UserApi, Configuration } from 'nhitomi-api'
import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'
import { ValidationError } from './validationError'
import { ConfigManager } from './config'

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

        // validation failure (unprocessable entity)
        if (response.status === 422) {
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
  public user!: UserApi

  /** Info API */
  public info!: InfoApi

  /** Book API. */
  public book!: BookApi

  /** Configuration manager. */
  public config!: ConfigManager

  /** Contains client and API information. */
  public currentInfo!: GetInfoResponse

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

    this.config = new ConfigManager(this)
    this.config.on('token', () => { window.location.reload() })
    this.config.on('baseUrl', () => { window.location.reload() })

    this.httpConfig.accessToken = this.config.token
    this.httpConfig.basePath = this.config.baseUrl
  }

  /** Initializes this client. */
  public async initialize() {
    this.currentInfo = await this.info.getInfo()
  }
}
