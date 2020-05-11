import { CommandFunc } from '.'
import { Api } from '../api'
import { ScraperCategory, BookQuery, BookSort, SortDirection } from 'nhitomi-api'
import { BookSearchMessage } from './search'
import { MessageContext } from '../context'
import { Message } from 'discord.js'

export function sourceInvalid(context: MessageContext, input: string): Promise<Message> {
  const l = context.locale.section('from.badSource')

  return context.reply(`
${l.get('message', { input })}

${Api.currentInfo.scrapers.map(s => `> - ${s.name} — <${s.url}>`).sort().join('\n')}
`.trim())
}

export const run: CommandFunc = async (context, source) => {
  const scraper = Api.currentInfo.scrapers.find(s => source && s.type.toString().toLowerCase().startsWith(source.toLowerCase()))

  switch (scraper?.category) {
    case ScraperCategory.Book: {
      const baseQuery: BookQuery = {
        source: {
          values: [scraper.type]
        },
        limit: 20,
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
