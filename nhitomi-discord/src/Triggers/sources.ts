import { ReactionTrigger, InteractiveMessage, RenderResult } from '../interactive'
import { MessageContext } from '../context'
import { Locale } from '../locales'
import { Book, BookContent, LanguageType } from 'nhitomi-api'
import { DestroyTrigger } from './destroy'
import { Api } from '../api'

export class BookSourcesMessage extends InteractiveMessage {
  constructor(
    readonly book: Book,
    readonly content: BookContent
  ) { super() }

  protected async render(l: Locale): Promise<RenderResult> {
    l = l.section('get.sources')

    const book = this.book
    const content = this.content

    return {
      embed: {
        title: book.primaryName,
        url: Api.getWebLink(`books/${book.id}/contents/${content.id}`),
        thumbnail: {
          url: Api.getApiLink(`books/${book.id}/contents/${content.id}/pages/-1`)
        },
        color: 'GREEN',
        author: {
          name: (book.tags.artist || book.tags.circle || [content.source]).sort().join(', '),
          iconURL: Api.getWebLink(`assets/icons/${content.source}.jpg`)
        },
        footer: {
          text: l.get('footer', { count: book.contents.length })
        },
        fields: Object.entries(
          book.contents.reduce((a, b) => {
            const k = `${Api.currentInfo.scrapers.find(s => s.type === b.source)?.name} (${b.language.split('-')[0]})`;
            (a[k] = a[k] || []).push(b)
            return a
          }, {} as Record<string, BookContent[]>)
        ).map(([source, contents]) => ({
          name: source,
          value: contents.map(c => {
            let url = c.sourceUrl

            if (c === content)
              url = `**${url}**`

            return url
          }).join('\n').slice(0, 1024)
        })).sort((a, b) => a.name.localeCompare(b.name)).slice(0, 25) // https://discordjs.guide/popular-topics/embeds.html#embed-limits
      }
    }
  }

  protected createTriggers(): ReactionTrigger[] {
    return [
      ...super.createTriggers(),
      new DestroyTrigger()
    ]
  }
}

export type SourcesTriggerTarget = {
  book?: Book
  content?: BookContent
}

export class SourcesTrigger extends ReactionTrigger {
  readonly emoji = '\ud83d\udd17'

  constructor(readonly target: SourcesTriggerTarget) { super() }

  protected async run(context: MessageContext) {
    const book = this.target.book
    const content = this.target.content

    if (book && content)
      return await new BookSourcesMessage(book, content).initialize(context)

    return false
  }
}
