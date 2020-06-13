import { CommandFunc } from '.'
import { Api } from '../api'
import { ScraperCategory, BookQuery, BookSort, SortDirection } from 'nhitomi-api'
import { BookSearchMessage } from './search'
import { MessageContext } from '../context'
import { Message } from 'discord.js'

export function sourceInvalid(context: MessageContext, input: string): Promise<Message> {
  const l = context.locale.section('from.badSource')

  return context.reply(`
${input && l.get('message', { input })}

${l.get('list')}
${Api.currentInfo.scrapers.map(s => `> - ${s.name} — <${s.url}>`).sort().join('\n')}
`.trim())
}

export const run: CommandFunc = async (context, source) => {
  const scraper = Api.currentInfo.scrapers.find(s => source && s.type.toLowerCase().startsWith(source.toLowerCase()))

  switch (scraper?.category) {
    case ScraperCategory.Book: {
      const baseQuery: BookQuery = {
        source: {
          values: [scraper.type]
        },
        limit: 0,
        sorting: [{
          value: BookSort.CreatedTime,
          direction: SortDirection.Descending
        }]
      }

      return new BookSearchMessage(baseQuery).initialize(context)
    }

    default: {
      await sourceInvalid(context, source || '')
      return true
    }
  }
}
