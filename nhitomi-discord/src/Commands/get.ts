import { CommandFunc } from '.'
import { InteractiveMessage, RenderResult } from '../interactive'
import { Locale } from '../locales'
import { Book, BookContent, BookTag } from 'nhitomi-api'
import { Api } from '../api'

export class BookMessage extends InteractiveMessage {
  constructor(
    readonly book: Book,
    public content: BookContent
  ) {
    super()
  }

  protected async render(l: Locale): Promise<RenderResult> {
    return BookMessage.renderStatic(l, this.book, this.content)
  }

  static renderStatic(l: Locale, book: Book, content: BookContent): RenderResult {
    l = l.section('get.book')

    return {
      message: Object.values(book.contents.reduce((a, b) => {
        const k = `${b.source}/${b.language}`;
        (a[k] = a[k] || []).push(b)
        return a
      }, {} as Record<string, BookContent[]>)).map(x => {
        const { source, language } = x[0]

        const urls = x.map(c => c.sourceUrl).sort()

        for (let i = 0; i < urls.length; i++)
          if (urls[i] === content.sourceUrl)
            urls[i] = `**${urls[i]}**`

        return `${source} (${language.toString().split('-')[0]}): ${urls.join(' ')}`
      }).sort().join('\n'),
      embed: {
        title: book.primaryName,
        description: book.englishName === book.primaryName ? undefined : book.englishName,
        url: Api.getWebLink(`books/${book.id}/contents/${content.id}?auth=discord`),
        image: {
          url: Api.getApiLink(`books/${book.id}/contents/${content.id}/pages/0/thumb`)
        },
        color: 'GREEN',
        author: {
          name: (book.tags.artist || book.tags.circle || [content.source]).join(', '),
          iconURL: Api.getWebLink(`assets/icons/${content.source}.jpg`)
        },
        footer: {
          text: `${book.id}/${content.id} (${l.section('categories').get(book.category.toString())})`
        },
        fields: (Object.keys(BookTag) as (keyof typeof book.tags)[]).sort().filter(t => book.tags[t]?.length).map(t => ({
          name: l.section('tags').get(t),
          value: book.tags[t]?.sort().join(', '),
          inline: true
        }))
      }
    }
  }
}

export const run: CommandFunc = async (context, link) => {
  if (link) {
    // try finding books
    const { body: { matches: [bookMatch] } } = await context.api.book.getBooksByLink(false, { link })

    if (bookMatch) {
      const { book, selectedContentId } = bookMatch
      const content = book.contents.find(c => c.id === selectedContentId)

      if (content)
        return await new BookMessage(book, content).initialize(context)
    }
  }

  const l = context.locale.section('get.notFound')

  await context.reply(`
${l.get('message', { input: link })}

> - ${l.get('usageLink', { example: 'https://nhentai.net/g/123/' })}
> - ${l.get('usageSource', { example: 'hitomi 123' })}`)

  return true
}
