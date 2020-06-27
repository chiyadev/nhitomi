import { BookContent, Client, Book } from '../Client'
import { createContext } from 'react'

export const FetchManagerContext = createContext<FetchManager>(undefined as any)

export type FetchImage =
  { index: number } & (
    { done: true, data: string, width: number, height: number } |
    { done: Error })

/** Responsible for fetching book images sequentially and asynchronously. */
export class FetchManager {
  images: (FetchImage | undefined)[] = []

  /** Images pending fetch. */
  readonly queue: { index: number }[] = []

  constructor(
    readonly client: Client,
    readonly book: Book,
    readonly content: BookContent,

    /** Maximum fetch concurrency (number of workers). */
    readonly concurrency: number,

    /** Callback when an image fetch was done. */
    private readonly onfetched: (images: (FetchImage | undefined)[]) => void
  ) {
    this.images = new Array(content.pageCount)

    for (let i = content.pageCount - 1; i >= 0; i--)
      this.queue.push({ index: i })
  }

  /** Number of workers currently running. */
  current = 0
  running = false
  destroyed = false

  start() {
    this.running = true

    for (let i = this.current; i < this.concurrency; i++)
      this.run()
  }

  stop() {
    this.running = false
  }

  retry(image: FetchImage) {
    const images = this.images.slice()
    images[image.index] = undefined

    this.queue.push(image) // insert back into queue at the start
    this.onfetched(this.images = images)

    this.start()
  }

  private async run() {
    ++this.current

    try {
      while (this.running && this.current <= this.concurrency) {
        const item = this.queue.pop()

        if (!item)
          break

        let done: FetchImage

        try {
          // fetch image
          const blob = await this.client.book.getBookImage({
            id: this.book.id,
            contentId: this.content.id,
            index: item.index
          })

          // probe metadata
          const { width, height } = this.client.image.detect(new Uint8Array(await new Response(blob).arrayBuffer()))

          if (this.destroyed) // may have been destroyed while fetching
            return

          done = {
            ...item,
            done: true,
            data: URL.createObjectURL(blob),
            width,
            height
          }
        }
        catch (e) {
          done = {
            ...item,
            done: e as Error
          }
        }

        const images = this.images.slice()
        images[done.index] = done

        this.onfetched(this.images = images)
      }
    }
    finally {
      --this.current
    }
  }

  destroy() {
    this.stop()
    this.destroyed = true

    for (const image of this.images)
      image?.done === true && URL.revokeObjectURL(image.data)

    this.queue.length = 0
    this.images.length = 0
  }
}
