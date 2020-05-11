import { ReactionTrigger } from '../interactive'
import { Book, BookContent } from 'nhitomi-api'
import { BookReadMessage } from '../Commands/view'

export type ReadTriggerTarget = {
  book?: Book
  content?: BookContent
}

export class ReadTrigger extends ReactionTrigger {
  readonly emoji = '\uD83D\uDCD6'

  constructor(readonly target: ReadTriggerTarget) { super() }

  protected async run(): Promise<boolean> {
    const book = this.target.book
    const content = this.target.content

    if (this.context && book && content)
      return await new BookReadMessage(book, content).initialize(this.context)

    return false
  }
}
