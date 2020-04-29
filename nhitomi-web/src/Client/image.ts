import { Client } from '.'

// @ts-ignore
import { sync as probeImage } from 'probe-image-size'

export type ImageSource = Uint8Array | Blob
export type ImageResizeMode = 'stretch' | 'fit' | 'fill'

type SizeObj = { width: number, height: number }

/** Contains functions for working with images asynchronously. */
export class ImageWorker {
  constructor(public readonly client: Client) { }

  /** Detects format and dimensions from the given image file. */
  public detect(buffer: Uint8Array): { width: number, height: number, type: string } {
    const { width, height, mime: type } = probeImage(buffer) || {}

    if (!type)
      throw Error('Unrecognized image format.')

    return { width, height, type }
  }

  /** Resizes the given image file. */
  public async resize(src: ImageSource, size: SizeObj | ((size: SizeObj) => SizeObj), mode: ImageResizeMode, type: string, quality: number) {
    let { image, canvas, context, data } = await this.loadPixels(src)

    let { width, height } = typeof size === 'object' ? size : size(image)

    let scale: number

    switch (mode) {
      case 'stretch': scale = (width / data.width + height / data.height) / 2; break
      case 'fit': scale = Math.min(width / data.width, height / data.height); break
      case 'fill': scale = Math.max(width / data.width, height / data.height); break
    }

    if (mode !== 'stretch') {
      width = Math.ceil(data.width * scale)
      height = Math.ceil(data.height * scale)
    }

    // use hermite resampling if scale < 1
    // if (scale < 1) {
    //   canvas.width = width
    //   canvas.height = height

    //   context.putImageData(await this.workers.use(w => w.hermite(data, width, height)), 0, 0)
    // }

    // otherwise use a second canvas to scale
    // else
    {
      const oldCanvas = canvas;

      ({ canvas, context } = this.createCanvas(width, height))

      context.drawImage(oldCanvas, 0, 0, width, height)
    }

    return {
      result: await this.serializeCanvas(canvas, type, quality),
      scale,
      width,
      height
    }
  }

  /** Loads an image source into an uint8array. */
  public async loadBuffer(src: ImageSource) {
    if (src instanceof Uint8Array)
      return src

    return new Uint8Array(await new Response(src).arrayBuffer())
  }

  /** Loads an image file as HTML image element. */
  public loadImage(src: ImageSource) {
    const blobSrc = src instanceof Uint8Array ? new Blob([src]) : src

    // use createImageBitmap on supported browsers
    if ('createImageBitmap' in window)
      return createImageBitmap(blobSrc)

    // use image element and blob URL as fallback
    return new Promise<HTMLImageElement>((resolve, reject) => {
      const image = new Image()

      image.onload = () => {
        URL.revokeObjectURL(image.src)
        resolve(image)
      }

      image.onerror = (_, __, ___, ____, error) => {
        URL.revokeObjectURL(image.src)
        reject(error || Error('Failed to load image data.'))
      }

      image.src = URL.createObjectURL(blobSrc)
    })
  }

  /** Loads a new canvas from an image file. */
  public async loadCanvas(src: ImageSource) {
    const image = await this.loadImage(src)

    const { canvas, context } = this.createCanvas(image.width, image.height)

    context.drawImage(image, 0, 0)

    return { image, canvas, context }
  }

  /** Loads pixel data from an image file. */
  public async loadPixels(src: ImageSource) {
    const { image, canvas, context } = await this.loadCanvas(src)

    return { image, canvas, context, data: context.getImageData(0, 0, canvas.width, canvas.height) }
  }

  /** Helper for creating a canvas element. */
  public createCanvas(width: number, height: number) {
    const canvas = document.createElement('canvas')

    canvas.width = width
    canvas.height = height

    const context = canvas.getContext('2d', {
      // https://developers.google.com/web/updates/2019/05/desynchronized
      // desynchronized: true
    })!

    context.imageSmoothingEnabled = true
    context.imageSmoothingQuality = 'high'

    return { canvas, context }
  }

  /** Serializes canvas data into an image file of the given format. */
  public serializeCanvas(canvas: HTMLCanvasElement, type: string, quality: number) {
    return new Promise<Blob>((resolve, reject) =>
      canvas.toBlob(b => {
        if (b)
          resolve(b)
        else
          reject(Error('canvas.toBlob gave null.'))
      }, type, quality))
  }

  /**
   * Lays the given image files side-by-side in a specific direction.
   * If some images are smaller than others, they will be scaled up to fit the width/height of the largest image based on the layout direction.
   */
  public async combine(direction: 'vertical' | 'horizontal', type: string, quality: number, ...src: ImageSource[]) {
    if (!src.length)
      throw Error('No images to combine.')

    if (src.length === 1) {
      const image = src[0]

      return image instanceof Uint8Array ? new Blob([image]) : image
    }

    const items = await Promise.all(src.map(async x => {
      const image = await this.loadImage(x)

      return {
        image,
        dx: 0,
        dy: 0,
        dw: image.width,
        dh: image.height
      }
    }))

    let width = 0
    let height = 0

    switch (direction) {
      case 'vertical':
        let y = 0

        width = items.reduce((a, b) => a.dw > b.dw ? a : b).dw

        for (const item of items) {
          item.dh *= width / item.dw
          item.dw = width
          item.dy = y

          y += item.dh
          height += item.dh
        }
        break

      case 'horizontal':
        let x = 0

        height = items.reduce((a, b) => a.dh > b.dh ? a : b).dh

        for (const item of items) {
          item.dw *= height / item.dh
          item.dh = height
          item.dx = x

          x += item.dw
          width += item.dw
        }
        break
    }

    const { canvas, context } = this.createCanvas(width, height)

    for (const { image, dx, dy, dw, dh } of items)
      context.drawImage(image, dx, dy, dw, dh)

    return await this.serializeCanvas(canvas, type, quality)
  }

  /** Crops the given image file, returning a portion of it. */
  public async crop(src: ImageSource, { x, y, width, height }: { x: number, y: number, width: number, height: number }, type: string, quality: number) {
    const image = await this.loadImage(src)

    const { canvas, context } = this.createCanvas(width, height)

    context.drawImage(image, x, y, width, height, 0, 0, width, height)

    const result = await this.serializeCanvas(canvas, type, quality)

    return result
  }
}
