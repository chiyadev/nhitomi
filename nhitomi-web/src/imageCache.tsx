type ImageCache = {
  blob: Blob
  width: number
  height: number
}

const cache: Record<string, ImageCache & { time: number }> = {}

export function getImageCache(key: string): ImageCache | undefined {
  const entry = cache[key]

  if (entry) {
    entry.time = Date.now()

    return entry
  }
}

export function setImageCache(key: string, image: ImageCache) {
  cache[key] = {
    ...image,
    time: Date.now()
  }

  optimize()
}

export const ImageCacheSize = 1024 * 1024 * 32 // 32 MiB

// removes items from the cache to below ImageCacheSize, preferring to keep more recently accessed items
function optimize() {
  const keys = Object.keys(cache).sort((a, b) => cache[b].time - cache[a].time)
  let totalSize = 0

  for (const key of keys) {
    const size = cache[key].blob.size

    if ((totalSize + size) <= ImageCacheSize)
      totalSize += size
    else
      delete cache[key]
  }
}
