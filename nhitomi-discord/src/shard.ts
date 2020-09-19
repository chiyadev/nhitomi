import './logging'
import { Client, Message } from 'discord.js-light'
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
import { register, collectDefaultMetrics, Histogram, Counter, Gauge } from 'prom-client'
import { getBuckets } from './metrics'
import { RegExpCache } from './regex'

collectDefaultMetrics({ register })

export const Discord = new Client({
  cacheGuilds: true, // required for prometheus guild metrics
  cacheChannels: false,
  cacheOverwrites: false,
  cacheRoles: false,
  cacheEmojis: false,
  cachePresences: false,

  fetchAllMembers: false,
  messageCacheMaxSize: 0,
  // partials: [ no partials discord.js-light
  //   'CHANNEL',
  //   'GUILD_MEMBER',
  //   'MESSAGE',
  //   'REACTION',
  //   'USER'
  // ],
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

const guildCount = new Gauge({
  name: 'discord_guilds',
  help: 'Number of guilds invited to.'
})

const updateGuildsMetric = () => guildCount.set(Discord.guilds.cache.size)

Discord.on('ready', updateGuildsMetric)
Discord.on('guildCreate', updateGuildsMetric)
Discord.on('guildDelete', updateGuildsMetric)

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
  // make channel fields (specifically 'nsfw') availble for the code within action
  await channel.fetch()

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
  name: 'discord_command',
  help: 'Time spent on executing commands.',
  buckets: getBuckets(0.05, 10, 12),
  labelNames: ['command']
})

const commandErrorCount = new Counter({
  name: 'discord_command_errors',
  help: 'Number of errors while executing commands.',
  labelNames: ['command']
})

Discord.on('message', wrapHandler('message', async message => {
  if (message.channel.type !== 'text') return

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

    const measure = commandTime.startTimer({ command: name })

    try {
      await whileTyping(message.channel, async () => {
        const context = await MessageContext.create(message)

        try {
          // ensure nsfw channel
          if (message.channel.type === 'text' && !message.channel.nsfw) {
            await context.reply(context.locale.get('nsfw.notAllowed'))
            return
          }

          console.debug(`user ${context.user.id} '${context.user.username}' executing command '${command}' with args '${arg || ''}'`)

          await run(context, arg)
        }
        catch (e) {
          if (e instanceof Error) {
            let stack = e.stack

            if (stack && stack.length > 1920)
              stack = stack.substring(0, 1920) + '...'

            await context.reply({
              embed: {
                title: context.locale.get('error.title'),
                color: 'RED',
                description: `
${context.locale.get('error.description')}

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
    // optimize api calls by first checking for the existence of links
    const content = message.content.trim()
    const shouldScan = content && Api.currentInfo.scrapers.some(s => !s.galleryRegexLax || content.match(RegExpCache.get(s.galleryRegexLax))?.length)

    if (!shouldScan)
      return

    const result = await Api.book.getBooksByLink({ strict: false, getBookByLinkRequest: { link: content } })

    if (!result.matches.length)
      return

    await whileTyping(message.channel, async () => {
      const context = await MessageContext.create(message)

      try {
        // ensure nsfw channel
        if (message.channel.type === 'text' && !message.channel.nsfw) {
          await context.reply(context.locale.get('nsfw.notAllowed'))
          return
        }

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
