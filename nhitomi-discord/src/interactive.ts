import { Message, MessageEmbed, MessageEmbedOptions, MessageReaction, PartialMessage, TextChannel, DMChannel, NewsChannel } from 'discord.js'
import { Lock } from 'semaphore-async-await'
import deepEqual from 'fast-deep-equal'
import config from 'config'

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

  /** Message that contains the command string. */
  command?: Message

  /** Message that contains the rendered interactive content. */
  reply?: Message

  /** Channel in which this interactive operates. */
  get channel(): TextChannel | DMChannel | NewsChannel | undefined { return this.reply?.channel || this.command?.channel }

  /** List of triggers that alter interactive state. */
  triggers?: ReactionTrigger[]

  private lastView: {
    message?: string
    embed?: MessageEmbed
  } = {}

  /** Initializes this interactive message. */
  initialize({ command, reply }: { command?: Message, reply?: Message }): Promise<boolean> {
    if (this.command) delete interactives[this.command.id]
    if (this.reply) delete interactives[this.reply.id]

    if ((this.command = command)) interactives[this.command.id] = this
    if ((this.reply = reply)) interactives[this.reply.id] = this

    return this.update()
  }

  /** Renders this interactive immediately. */
  async update(): Promise<boolean> {
    await this.lock.wait()
    try {
      this.timeout.refresh()

      const result = await this.render()
      const view = {
        message: result.message,
        embed: result.embed ? new MessageEmbed(result.embed) : undefined
      }

      if (!view.message && !view.embed)
        return false

      const lastReply = this.reply

      if (this.reply && this.reply.editable) {
        if (deepEqual(this.lastView, view)) {
          console.debug('skipping rendering for interactive', this.constructor.name, this.reply.id)
          return false
        }

        this.reply = await this.reply.edit(view.message, view.embed)
      }
      else {
        this.reply = await this.command?.channel.send(view.message, view.embed)
      }

      if (lastReply) delete interactives[lastReply.id]
      if (this.reply) {
        interactives[this.reply.id] = this

        if (this.reply.id !== lastReply?.id)
          for (const trigger of this.triggers = this.createTriggers()) {
            trigger.interactive = this
            trigger.reaction = await this.reply.react(trigger.emoji)
          }
      }

      if (this.reply) console.debug('rendered interactive', this.constructor.name, this.reply.id)

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
  protected abstract render(): Promise<RenderResult>

  /**
   * Destroys this interactive, deleting all related messages.
   * @param expiring true if interactive is being destroyed because it expired
   */
  async destroy(expiring?: boolean): Promise<void> {
    await this.lock.wait()
    try {
      if (this.reply) console.debug('destroying interactive', this.constructor.name, this.reply.id, 'expiring', expiring || false)

      try { if (!expiring && this.command?.deletable) await this.command.delete() }
      catch { /* ignored */ }

      if (this.command) delete interactives[this.command.id]
      this.command = undefined

      try { if (!expiring && this.reply?.deletable) await this.reply.delete() }
      catch { /* ignored */ }

      if (this.reply) delete interactives[this.reply.id]
      this.reply = undefined
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

export async function handleInteractiveReaction(reaction: MessageReaction): Promise<boolean> {
  const interactive = getInteractive(reaction.message)

  if (!interactive)
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

  /** Runs this trigger immediately. */
  async invoke(): Promise<boolean> {
    const interactive = this.interactive

    if (!interactive?.reply)
      return false

    await interactive.lock.wait()
    try {
      console.log('invoking trigger', this.emoji, 'for interactive', interactive.constructor.name, interactive.reply.id)

      return await this.run()
    }
    finally {
      interactive.lock.signal()
    }
  }

  /** Alters the state of the interactive while the message is locked. */
  protected abstract run(): Promise<boolean>
}
