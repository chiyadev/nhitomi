import { Client as DiscordClient } from 'discord.js'
import config from 'config'
import { CommandModule } from './Commands'
import { shouldHandleMessage } from './filter'
import { handleInteractiveMessage, handleInteractiveReaction, handleInteractiveMessageDeleted } from './interactive'

export const Client = new DiscordClient({
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

Client.on('debug', console.debug)
Client.on('warn', console.warn)
Client.on('error', console.error)

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

Client.on('message', wrapHandler('message', async message => {
  if (!await shouldHandleMessage(message)) return
  if (await handleInteractiveMessage(message)) return

  const prefix = config.get<string>('prefix')

  if (!message.content.startsWith(prefix)) return

  const content = message.content.substring(prefix.length).trim()
  const space = content.indexOf(' ')
  const command = space === -1 ? content : content.substring(0, space)
  const arg = space === -1 ? undefined : content.substring(space + 1).trim()

  let module: CommandModule

  try {
    module = await import(`./Commands/${command}`)
  }
  catch (e) {
    console.debug('command exec error', e)
    return
  }

  console.log(`executing command '${command}' with args '${arg || ''}'`)

  await module.run(message, arg)
}))

Client.on('messageDelete', wrapHandler('messageDelete', async message => {
  await handleInteractiveMessageDeleted(message)
}))

Client.on('messageReactionAdd', wrapHandler('messageReactionAdd', async reaction => {
  await handleInteractiveReaction(reaction)
}))

Client.on('messageReactionRemove', wrapHandler('messageReactionRemove', async reaction => {
  await handleInteractiveReaction(reaction)
}))

Client.login(config.get('token'))
