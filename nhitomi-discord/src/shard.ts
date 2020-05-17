import './logging'
import { Client } from 'discord.js'
import config from 'config'
import { loadCommands, matchCommand } from './Commands'
import { shouldHandleMessage, shouldHandleReaction } from './filter'
import { handleInteractiveMessage, handleInteractiveReaction, handleInteractiveMessageDeleted } from './interactive'
import { MessageContext } from './context'
import { Api } from './api'
import { beginPresenceRotation } from './status'

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

Discord.on('message', wrapHandler('message', async message => {
  if (!await shouldHandleMessage(message)) return
  if (await handleInteractiveMessage(message)) return

  const prefix = config.get<string>('prefix')

  if (!message.content.startsWith(prefix)) return

  const content = message.content.substring(prefix.length).trim()
  const space = content.indexOf(' ')
  const command = space === -1 ? content : content.substring(0, space)
  const arg = space === -1 ? undefined : content.substring(space + 1).trim()

  const func = matchCommand(command)

  if (!func)
    return

  let typing = true
  message.channel.startTyping()

  // max typing time 3s
  setTimeout(() => { message.channel.stopTyping(); typing = false }, 3000)

  try {
    const context = await MessageContext.create(message)

    try {
      console.debug(`user ${context.user.id} '${context.user.username}' executing command '${command}' with args '${arg || ''}'`)

      await func(context, arg)
    }
    finally {
      context.destroy()
    }
  }
  finally {
    if (typing)
      message.channel.stopTyping()
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
