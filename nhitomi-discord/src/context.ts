import { Locale } from './locales'
import { User } from 'nhitomi-api'
import { ApiClient, Api } from './api'
import { Message } from 'discord.js-light'
import NodeCache from 'node-cache'
import config from 'config'
import { InteractiveMessage, HeadlessInteractiveMessage } from './interactive'

const tokenCache = new NodeCache({
  stdTTL: config.get<number>('api.cachedTokenExpiry')
})

/** Context in which messages are handled. */
export class MessageContext {
  /** Locale to format output messages. */
  readonly locale: Locale

  private constructor(
    /** Message being handled. */
    readonly message: Message,

    /** API client that makes requests on behalf of the user. */
    readonly api: ApiClient,

    /** API user information. */
    readonly user: User
  ) {
    this.locale = Locale.get(user.language)
    this.reply = message.channel.send.bind(message.channel)
  }

  refCount = 1

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

    if (count === 0) {
      this.api.destroy()

      console.log('context destroyed for message', this.message.id)
    }
  }

  /** Shorthand for `message.channel.send(...)`. */
  reply: Message['channel']['send']

  /** Schedules the deletion of the given message and returns a promise that resolves when it is deleted. Promise will never reject. */
  async scheduleDelete(message: Message, timeout = config.get<number>('interactive.notifTimeout')): Promise<void> {
    await new Promise(r => setTimeout(r, timeout * 1000))

    if (message.deletable)
      try {
        await message.delete()
      }
      catch (e) {
        console.debug('could not delete message', message.id, e)
      }
  }

  /** Creates a headless interactive and calls waitInput on it. */
  waitInput: InteractiveMessage['waitInput'] = async (content, timeout) => {
    const interactive = new HeadlessInteractiveMessage()

    try {
      await interactive.initialize(this)

      return await interactive.waitInput(content, timeout)
    }
    finally {
      interactive.destroy()
    }
  }

  /** Creates a message context from a message. */
  static async create(message: Message): Promise<MessageContext> {
    const author = message.author
    const cachedToken = tokenCache.get<string>(author.id)

    if (cachedToken) {
      const api = new ApiClient(cachedToken)

      try {
        const user = await api.user.getSelfUser()

        return new MessageContext(message, api, user)
      }
      catch (e) {
        api.destroy()
        console.debug('message context error using cached token', cachedToken, e)
      }
    }

    const { token, user } = await Api.internal.getOrCreateUserDiscord({
      getOrCreateDiscordUserRequest: {
        id: author.id,
        username: author.username,
        discriminator: parseInt(author.discriminator)
      }
    })

    tokenCache.set(author.id, token)

    return new MessageContext(message, new ApiClient(token), user)
  }
}
