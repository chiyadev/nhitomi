import { BookContent, BookImage, Client, Book } from '../Client'
import { createContext } from 'react'

export const FetchManagerContext = createContext<FetchManager>(undefined as any)

export type FetchImage =
  { image: BookImage, index: number } & (
    { done: true, data: string, width: number, height: number } |
    { done: Error })

/** Responsible for fetching book images sequentially and asynchronously. */
export class FetchManager {
  public images: (FetchImage | undefined)[] = []

  /** Images pending fetch. */
  public readonly queue: { image: BookImage, index: number }[]

  constructor(
    public readonly client: Client,
    public readonly book: Book,
    public readonly content: BookContent,

    /** Maximum fetch concurrency (number of workers). */
    public readonly concurrency: number,

    /** Callback when an image fetch was done. */
    private readonly onfetched: (images: (FetchImage | undefined)[]) => void
  ) {
    this.images = content.pages.map(() => undefined)
    this.queue = content.pages.map((image, index) => ({ image, index })).reverse()
  }

  /** Number of workers currently running. */
  public current = 0
  public running = false
  public destroyed = false

  public start() {
    this.running = true

    for (let i = this.current; i < this.concurrency; i++)
      this.run()
  }

  public stop() {
    this.running = false
  }

  public retry(image: FetchImage) {
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
            done: true,
            data: URL.createObjectURL(blob),
            width,
            height,
            ...item
          }
        }
        catch (e) {
          done = {
            done: e as Error,
            ...item
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

  public destroy() {
    this.stop()
    this.destroyed = true

    for (const image of this.images)
      image?.done === true && URL.revokeObjectURL(image.data)

    this.queue.length = 0
    this.images.length = 0
  }
}
