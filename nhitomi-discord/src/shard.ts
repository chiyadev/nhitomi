import { Client as DiscordClient } from 'discord.js'
import config from 'config'

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
Client.on('message', m => console.log('message', m))

Client.login(config.get('token'))
