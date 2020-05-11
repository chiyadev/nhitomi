/** An array that loads items asynchronously in chunks and caches them in memory. Used to display paginated search results. */
export class AsyncArray<T> {
  readonly cache: T[] = []

  get loadedLength(): number {
    for (let i = this.cache.length - 1; i >= 0; i--)
      if (this.cache[i])
        return i + 1

    return 0
  }

  constructor(
    readonly chunkSize: number,
    readonly onload: (offset: number, limit: number) => Promise<T[]>
  ) { }

  async get(index: number): Promise<T | undefined> {
    const current = this.cache[index]

    if (current)
      return current

    const start = Math.floor(index / this.chunkSize) * this.chunkSize
    const loaded = await this.onload(start, this.chunkSize)

    for (let i = 0; i < loaded.length; i++)
      this.cache[start + i] = loaded[i]

    return this.cache[index]
  }

  reset(): void { this.cache.length = 0 }
}
