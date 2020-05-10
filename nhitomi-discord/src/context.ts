import { Locale } from './locales'
import { User } from 'nhitomi-api'
import { ApiClient } from './api'
import { Message } from 'discord.js'

/** Context in which messages are handled. */
export class MessageContext {
  /** API client to make requests on behald of the user. */
  readonly api: ApiClient

  /** Locale to format output messages. */
  readonly locale: Locale

  /** Shorthand for `locale.l(...)`. */
  l!: Locale['l']

  constructor(
    /** Message to be handled. */
    readonly message: Message,

    /** API client token. */
    token: string,

    /** API user information. */
    readonly user: User
  ) {
    this.api = ApiClient.pool.rent(token)

    this.locale = Locale.get(user.language)
    this.l = this.locale.l.bind(this.locale)
  }

  private refCount = 1

  /**
   * Increments the ref count of this context.
   * Ref counting is used to keep this context alive beyond the scope of the message handler (e.g. for interactive messages).
   */
  ref(): this {
    ++this.refCount
    return this
  }

  /** Decrements the ref count of this context, and destroys it if the count is zero. */
  destroy(): void {
    const count = --this.refCount

    if (count === 0)
      ApiClient.pool.return(this.api)
  }
}
