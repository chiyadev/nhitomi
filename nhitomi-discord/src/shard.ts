import './logging'
import { Client, Message } from 'discord.js'
import config from 'config'
import { loadCommands, matchCommand } from './Commands'
import { shouldHandleMessage, shouldHandleReaction } from './filter'
import { handleInteractiveMessage, handleInteractiveReaction, handleInteractiveMessageDeleted } from './interactive'
import { MessageContext } from './context'
import { Api } from './api'
import { beginPresenceRotation } from './status'
import { BookListMessage } from './Commands/search'
import { AsyncArray } from './asyncArray'
import { BookMessage } from './Commands/get'
import { register, collectDefaultMetrics, Histogram, Counter } from 'prom-client'
import { getBuckets, measureHistogram } from './metrics'

collectDefaultMetrics({ register })

export const Discord = new Client({
  fetchAllMembers: false,
  messageCacheMaxSize: 0,
  partials: [
    'CHANNEL',
    'GUILD_MEMBER',
    'MESSAGE',
    'REACTION',
    'USER'
  ],
  ws: {
    intents: [
      'GUILDS',
      'GUILD_MESSAGES',
      'GUILD_MESSAGE_REACTIONS',
      'DIRECT_MESSAGES',
      'DIRECT_MESSAGE_REACTIONS'
    ],
    'large_threshold': 1
  }
})

Discord.on('debug', console.debug)
Discord.on('warn', console.warn)
Discord.on('error', console.error)

Discord.on('ready', () => register.setDefaultLabels({ shard: Discord.shard?.ids[0]?.toString() }))

function wrapHandler<T extends Function>(name: string, func: T): T {
  return (async (...args: unknown[]) => {
    try {
      return await func(...args)
    }
    catch (e) {
      console.warn('unhandled error in', name, 'handler', e)
    }
  }) as unknown as T
}

async function whileTyping<T>(channel: Message['channel'], action: () => Promise<T>): Promise<T> {
  let typing = true
  channel.startTyping()

  // max typing time 3s
  setTimeout(() => { channel.stopTyping(); typing = false }, 3000)

  try {
    return await action()
  }
  finally {
    if (typing)
      channel.stopTyping()
  }
}

const messageCount = new Counter({
  name: 'discord_messages',
  help: 'Number of messages received.'
})

const commandTime = new Histogram({
  name: 'discord_command_milliseconds',
  help: 'Time spent on executing commands.',
  buckets: getBuckets(50, 5000, 10),
  labelNames: ['command']
})

const commandErrorCount = new Counter({
  name: 'discord_command_errors',
  help: 'Number of errors while executing commands.',
  labelNames: ['command']
})

Discord.on('message', wrapHandler('message', async message => {
  messageCount.inc()

  if (!await shouldHandleMessage(message)) return
  if (await handleInteractiveMessage(message)) return

  const prefix = config.get<string>('prefix')

  // message is a command
  if (message.content.startsWith(prefix)) {
    const content = message.content.substring(prefix.length).trim()
    const space = content.indexOf(' ')
    const command = space === -1 ? content : content.substring(0, space)
    const arg = space === -1 ? undefined : content.substring(space + 1).trim()

    const module = matchCommand(command)

    if (!module)
      return

    const { name, run } = module

    const measure = measureHistogram(commandTime, { command: name })

    try {
      await whileTyping(message.channel, async () => {
        const context = await MessageContext.create(message)

        try {
          console.debug(`user ${context.user.id} '${context.user.username}' executing command '${command}' with args '${arg || ''}'`)

          await run(context, arg)
        }
        catch (e) {
          if (e instanceof Error) {
            const l = context.locale.section('error')

            let stack = e.stack

            if (stack && stack.length > 1920)
              stack = stack.substring(0, 1920) + '...'

            await context.reply({
              embed: {
                title: l.get('title'),
                color: 'RED',
                description: `
${l.get('description')}

\`\`\`
${stack}
\`\`\`
`.trim(),
                footer: {
                  text: `${context.user.username} (${context.user.id})`
                }
              }
            })
          }

          commandErrorCount.inc({ command: name })
        }
        finally {
          context.destroy()
        }
      })
    }
    catch (e) {
      commandErrorCount.inc({ command: name })
      throw e
    }
    finally {
      measure()
    }
  }

  // scan message for links (this is slightly different from n!get command because strict=false; allows multiple links)
  else {
    const result = await Api.book.getBooksByLink({ strict: false, getBookByLinkRequest: { link: message.content } })

    if (!result.matches.length)
      return

    await whileTyping(message.channel, async () => {
      const context = await MessageContext.create(message)

      try {
        if (result.matches.length === 1) {
          const { book, selectedContentId } = result.matches[0]

          await new BookMessage(book, book.contents.find(c => c.id === selectedContentId) || book.contents[0]).initialize(context)
        }
        else {
          await new BookListMessage(AsyncArray.fromArray(result.matches.map(m => m.book))).initialize(context)
        }
      }
      finally {
        context.destroy()
      }
    })
  }
}))

Discord.on('messageDelete', wrapHandler('messageDelete', async message => {
  await handleInteractiveMessageDeleted(message)
}))

Discord.on('messageReactionAdd', wrapHandler('messageReactionAdd', async (reaction, user) => {
  if (await shouldHandleReaction(reaction))
    await handleInteractiveReaction(reaction, user)
}))

Discord.on('messageReactionRemove', wrapHandler('messageReactionRemove', async (reaction, user) => {
  if (await shouldHandleReaction(reaction))
    await handleInteractiveReaction(reaction, user)
}))

beginPresenceRotation();

(async (): Promise<void> => {
  let die = false

  if (!die)
    try { await loadCommands() }
    catch (e) { die = true; console.error('could not load commands', e) }

  if (!die)
    try { await Api.initialize() }
    catch (e) { die = true; console.error('could not initialize api client', e) }

  if (!die)
    try { await Discord.login(config.get('token')) }
    catch (e) { die = true; console.error('could not start discord client', e) }

  if (die)
    setTimeout(() => process.exit(1), 5000)
})()
