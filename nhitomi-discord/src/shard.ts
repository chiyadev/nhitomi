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

Discord.on('message', wrapHandler('message', async message => {
  if (!await shouldHandleMessage(message)) return
  if (await handleInteractiveMessage(message)) return

  const prefix = config.get<string>('prefix')

  // message is a command
  if (message.content.startsWith(prefix)) {
    const content = message.content.substring(prefix.length).trim()
    const space = content.indexOf(' ')
    const command = space === -1 ? content : content.substring(0, space)
    const arg = space === -1 ? undefined : content.substring(space + 1).trim()

    const func = matchCommand(command)

    if (!func)
      return

    await whileTyping(message.channel, async () => {
      const context = await MessageContext.create(message)

      try {
        console.debug(`user ${context.user.id} '${context.user.username}' executing command '${command}' with args '${arg || ''}'`)

        await func(context, arg)
      }
      catch (e) {
        if (e instanceof Error) {
          const l = context.locale.section('error')

          await context.reply({
            embed: {
              title: l.get('title'),
              color: 'RED',
              description: `
${l.get('description')}

\`\`\`
${e.stack}
\`\`\`
`.trim(),
              footer: {
                text: `${context.user.username} (${context.user.id})`
              }
            }
          })
        }
      }
      finally {
        context.destroy()
      }
    })
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
