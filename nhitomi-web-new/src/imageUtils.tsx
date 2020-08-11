export function getImageSize(data: Blob): Promise<{ width: number, height: number }> {
  if (typeof createImageBitmap === 'function') {
    return (async () => {
      const image = await createImageBitmap(data)

      const width = image.width
      const height = image.height

      image.close()

      return { width, height }
    })()
  }
  else {
    return new Promise((resolve, reject) => {
      const image = new Image()
      const src = URL.createObjectURL(data)

      const cleanup = () => URL.revokeObjectURL(src)

      image.onload = () => {
        resolve({ width: image.width, height: image.height })
        cleanup()
      }

      image.onerror = error => {
        reject(typeof error === 'string' ? Error(error) : Error(error.type))
        cleanup()
      }

      image.src = src
    })
  }
}
