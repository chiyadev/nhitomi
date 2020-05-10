import { Client as DiscordClient } from 'discord.js'
import config from 'config'
import { CommandModule } from './Commands'

export const Client = new DiscordClient({
  fetchAllMembers: false,
  messageCacheMaxSize: 0,
  partials: ['GUILD_MEMBER', 'CHANNEL'],
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

Client.on('debug', x => console.debug(x))

Client.on('message', async message => {
  const prefix = config.get<string>('prefix')

  if (!message.content.startsWith(prefix))
    return

  const content = message.content.substring(prefix.length).trim()
  const space = content.indexOf(' ')
  const command = space === -1 ? content : content.substring(0, space)
  const arg = space === -1 ? undefined : content.substring(space + 1).trim()

  let module: CommandModule

  try {
    module = await import(`./Commands/${command}`)
  }
  catch{ return }

  await module.run(message, arg)
})

Client.login(config.get('token'))
