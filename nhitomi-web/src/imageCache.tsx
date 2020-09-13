type ImageData = {
  blob: Blob
  width: number
  height: number
}

type CacheEntry = ImageData & {
  url: string
  refs: number
  time: number
}

const cache: Record<string, CacheEntry> = {}

/** Caller must call returnCachedImageRef when done. */
export function createCachedImageRef(key: string, image: ImageData): string {
  const url = URL.createObjectURL(image.blob)

  cache[key] = {
    ...image,
    url,
    refs: 1,
    time: Date.now()
  }

  return url
}

/** Caller must call returnCachedImageRef when done. */
export function getCachedImageRef(key: string): CacheEntry | undefined {
  const entry = cache[key]

  if (entry) {
    entry.refs++
    entry.time = Date.now()

    return entry
  }
}

export function returnCachedImageRef(key: string) {
  const entry = cache[key]

  if (entry) {
    entry.refs--
    entry.time = Date.now()
  }
}

const CacheSize = 1024 * 1024 * 32 // 32 MiB

setInterval(() => {
  const keys = Object.keys(cache).sort((a, b) => cache[b].time - cache[a].time)
  let totalSize = 0

  for (const key of keys) {
    const entry = cache[key]

    if ((totalSize + entry.blob.size) <= CacheSize) {
      totalSize += entry.blob.size
    }
    else if (!entry.refs) {
      URL.revokeObjectURL(entry.url)

      delete cache[key]
    }
  }
}, 1000)
