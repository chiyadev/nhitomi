/** An array that loads items asynchronously in chunks and caches them in memory. Used to display paginated search results. */
export class AsyncArray<T> {
  readonly cache: ({ loaded: true, value: T | undefined } | undefined)[] = []

  get loadedLength(): number {
    for (let i = this.cache.length - 1; i >= 0; i--)
      if (this.cache[i])
        return i + 1

    return 0
  }

  constructor(
    readonly chunkSize: number,
    readonly fetch: (offset: number, limit: number) => Promise<T[]>
  ) { }

  async get(index: number): Promise<T | undefined> {
    let current = this.cache[index]

    if (current)
      return current.value

    const start = Math.floor(index / this.chunkSize) * this.chunkSize
    const loaded = await this.fetch(start, this.chunkSize)

    for (let i = 0; i < Math.max(loaded.length, this.chunkSize); i++)
      this.cache[start + i] = { loaded: true, value: loaded[i] }

    current = this.cache[index]

    if (current)
      return current.value

    this.cache[index] = { loaded: true, value: undefined }
  }

  getCached(index: number): T | undefined {
    const current = this.cache[index]

    if (current)
      return current.value
  }

  reset(): void { this.cache.length = 0 }
}
