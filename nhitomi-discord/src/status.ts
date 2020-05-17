import { Discord } from './shard'
import config from 'config'

// todo: random books as status

export function beginPresenceRotation(): void {
  Discord.on('ready', async () => {
    const update = async (): Promise<void> => {
      const adjs = config.get<string>('status.adjectives').split(';')
      const adj = adjs[Math.floor(Math.random() * adjs.length)]

      const nouns = config.get<string>('status.nouns').split(';')
      const noun = nouns[Math.floor(Math.random() * nouns.length)]

      try {
        await Discord.user?.setPresence({
          activity: {
            name: config.get<string>('status.format').replace('adjective', adj).replace('noun', noun),
            type: 'PLAYING'
          }
        })
      }
      catch (e) {
        console.debug('could not update presence', e)
      }
    }

    await update()
    setInterval(update, config.get<number>('status.interval') * 1000)
  })
}
