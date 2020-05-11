import { CommandFunc } from '.'
import { InteractiveMessage, RenderResult, ReactionTrigger } from '../interactive'
import { Locale } from '../locales'
import { ListTrigger } from '../Triggers/list'
import { Api } from '../api'
import { MessageEmbedOptions } from 'discord.js'
import config from 'config'

type Page = 'doujinshi' | 'collections' | 'oss'
const Pages: Page[] = ['doujinshi', 'collections', 'oss']

class HelpMessage extends InteractiveMessage {
  position = 0

  get page(): Page {
    return Pages[this.position = Math.max(0, Math.min(Pages.length - 1, this.position))]
  }

  protected async render(l: Locale): Promise<RenderResult> {
    l = l.section('help')

    const embed: MessageEmbedOptions = {
      title: `**nhitomi**: ${l.get('title')}`,
      color: 'PURPLE',
      thumbnail: {
        url: 'https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png'
      },
      footer: {
        text: `v${Api.currentInfo.version.shortHash} — ${l.get('owner')}`
      }
    }

    l = l.section(this.page)
    const prefix = config.get<string>('prefix')

    switch (this.page) {
      case 'doujinshi':
        embed.fields = [{
          name: l.get('title'),
          value: `
- \`${prefix}get [link]\` — ${l.get('get')}
- \`${prefix}from [source]\` — ${l.get('from')}
- \`${prefix}search [query]\` — ${l.get('search')}
- \`${prefix}view [link]\` — ${l.get('view')}
- \`${prefix}download [link]\` — ${l.get('download')}
`.trim()
        }, {
          name: l.get('sources.title'),
          value: 'none yet'
        }]
        break

      case 'collections':
        embed.fields = [{
          name: l.get('title'),
          value: `
- \`${prefix}collection\` — ${l.get('list')}
- \`${prefix}collection [name]\` — ${l.get('show')}
- \`${prefix}collection [name] add [link]\` — ${l.get('add')}
- \`${prefix}collection [name] remove [link]\` — ${l.get('remove')}
- \`${prefix}collection [name] delete [link]\` — ${l.get('delete')}
`.trim()
        }]
        break

      case 'oss':
        embed.fields = [{
          name: l.get('title'),
          value: `
${l.get('license')}
[GitHub](https://github.com/chiyadev/nhitomi) / [License](https://github.com/chiyadev/nhitomi/blob/master/LICENSE)
`.trim()
        }]
        break
    }

    return { embed }
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),

      new ListTrigger(this, 'left'),
      new ListTrigger(this, 'right')
    ]
  }
}

export const run: CommandFunc = context => new HelpMessage().initialize(context)
