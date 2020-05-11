import { Message, MessageEmbed, MessageEmbedOptions, MessageReaction, PartialMessage, TextChannel, DMChannel, NewsChannel, User, PartialUser } from 'discord.js'
import { Lock } from 'semaphore-async-await'
import deepEqual from 'fast-deep-equal'
import config from 'config'
import { MessageContext } from './context'
import { Locale } from './locales'

const interactives: { [id: string]: InteractiveMessage } = {}
export function getInteractive(message: Message | PartialMessage): InteractiveMessage | undefined { return interactives[message.id] }

export type RenderResult = {
  message?: string
  embed?: MessageEmbedOptions
}

/** Represents an interactive, stateful message. */
export abstract class InteractiveMessage {
  /** Lock must be consumed during all stateful operation on this message. */
  readonly lock: Lock = new Lock()

  /** Timeout responsible for destroying this interactive after a delay. */
  readonly timeout = setTimeout(() => this.destroy(true), config.get<number>('interactive.timeout') * 1000)

  /** Context of the command message. */
  context?: MessageContext

  /** Message that contains the rendered interactive content. */
  rendered?: Message

  /** Channel in which this interactive operates. */
  get channel(): TextChannel | DMChannel | NewsChannel | undefined { return this.rendered?.channel || this.context?.message.channel }

  /** List of triggers that alter interactive state. */
  triggers?: ReactionTrigger[]

  private lastView: {
    message?: string
    embed?: MessageEmbed
  } = {}

  /** Initializes this interactive message. Context need not be ref'ed. */
  initialize(context: MessageContext): Promise<boolean> {
    if (this.context) {
      delete interactives[this.context.message.id]
      this.context.destroy()
    }

    this.context = context.ref()
    interactives[this.context.message.id] = this

    if (this.rendered) {
      delete interactives[this.rendered.id]
      this.rendered = undefined
    }

    return this.update()
  }

  /** Renders this interactive immediately. */
  async update(): Promise<boolean> {
    await this.lock.wait()
    try {
      this.timeout.refresh()

      const result = await this.render(this.context?.locale ?? Locale.default)
      const view = {
        message: result.message,
        embed: result.embed ? new MessageEmbed(result.embed) : undefined
      }

      if (!view.message && !view.embed)
        return false

      const lastRendered = this.rendered

      if (this.rendered?.editable) {
        if (deepEqual(this.lastView, view)) {
          console.debug('skipping rendering for interactive', this.constructor.name, this.rendered.id)
          return false
        }

        this.rendered = await this.rendered.edit(view.message, view.embed)
      }
      else {
        this.rendered = await this.context?.reply(view.message, view.embed)
      }

      if (lastRendered) delete interactives[lastRendered.id]
      if (this.rendered) {
        interactives[this.rendered.id] = this

        console.debug('rendered interactive', this.constructor.name, this.rendered.id)

        if (this.rendered.id !== lastRendered?.id)
          for (const trigger of this.triggers = this.createTriggers()) {
            trigger.interactive = this
            trigger.reaction = await this.rendered.react(trigger.emoji)
          }
      }

      this.lastView = view
      return true
    }
    finally {
      this.lock.signal()
    }
  }

  /** Creates a list of triggers that alter the state of this interactive. */
  protected createTriggers(): ReactionTrigger[] { return [] }

  /** Constructs a new view of this interactive. */
  protected abstract render(l: Locale): Promise<RenderResult>

  /**
   * Destroys this interactive, deleting all related messages.
   * @param expiring true if interactive is being destroyed because it expired
   */
  async destroy(expiring?: boolean): Promise<void> {
    await this.lock.wait()
    try {
      if (this.rendered) console.debug('destroying interactive', this.constructor.name, this.rendered.id, 'expiring', expiring || false)

      try { if (!expiring && this.context?.message.deletable) await this.context.message.delete() }
      catch { /* ignored */ }

      if (this.context) {
        delete interactives[this.context.message.id]
        this.context.destroy()
        this.context = undefined
      }

      try { if (!expiring && this.rendered?.deletable) await this.rendered.delete() }
      catch { /* ignored */ }

      if (this.rendered) {
        delete interactives[this.rendered.id]
        this.rendered = undefined
      }

      this.triggers = undefined
    }
    finally {
      this.lock.signal()
    }
  }
}

export async function handleInteractiveMessage(message: Message | PartialMessage): Promise<boolean> {
  const interactive = getInteractive(message)

  if (!interactive)
    return false

  return true
}

export async function handleInteractiveMessageDeleted(message: Message | PartialMessage): Promise<boolean> {
  const interactive = getInteractive(message)

  if (!interactive)
    return false

  await interactive.destroy()
  return true
}

export async function handleInteractiveReaction(reaction: MessageReaction, user: User | PartialUser): Promise<boolean> {
  const interactive = getInteractive(reaction.message)

  if (!interactive)
    return false

  // reactor must be command author
  if (user.id !== interactive.context?.message.author.id)
    return false

  const trigger = interactive.triggers?.find(t => t.emoji === reaction.emoji.name)

  if (!trigger)
    return false

  return await trigger.invoke()
}

/** Represents an interactive trigger that is invoked via message reactions. */
export abstract class ReactionTrigger {
  abstract readonly emoji: string

  interactive?: InteractiveMessage
  reaction?: MessageReaction

  get context(): MessageContext | undefined { return this.interactive?.context }

  /** Runs this trigger immediately. */
  async invoke(): Promise<boolean> {
    const interactive = this.interactive

    if (!interactive?.rendered)
      return false

    let result: boolean

    await interactive.lock.wait()
    try {
      console.debug('invoking trigger', this.emoji, 'for interactive', interactive.constructor.name, interactive.rendered.id)

      result = await this.run()
    }
    finally {
      interactive.lock.signal()
    }

    if (result)
      result = await interactive.update()

    return result
  }

  /** Alters the state of the interactive while the message is locked. Returning true will rerender the interactive. */
  protected abstract run(): Promise<boolean>
}
