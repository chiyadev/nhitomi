// tslint:disable: prefer-for-of

import { FetchImage } from './fetchManager'
import { Book, BookContent } from '../Client'
import { createContext } from 'react'

export const LayoutManagerContext = createContext<LayoutManager>(undefined as any)

export type LayoutImage = {
  image?: FetchImage
  x: number
  y: number
  width: number
  height: number
}

export type LayoutResult = {
  layout: LayoutImage[]
  width: number
  height: number

  /** describes what caused layout to be recalculated. */
  cause: 'variable' | 'images'
}

/** Responsible for calculating the layout of pages. */
export class LayoutManager {
  private lastResult: LayoutImage[]
  private lastImages?: (FetchImage | undefined)[]

  constructor(
    public readonly book: Book,
    public readonly content: BookContent
  ) {
    this.lastResult = content.pages.map(() => ({
      x: 0,
      y: 0,
      width: 0,
      height: 0
    }))
  }

  /**
   * Returns an array representing the new layout.
   * This function is stateful.
   * If a layout image has not changed appearance since the last calculation, it will have the same identity.
   */
  public recalculate(images: (FetchImage | undefined)[], {
    viewportWidth,
    viewportHeight,
    viewportBound = true,
    defaultImageAspect = 5 / 7,
    leftToRight = false,
    itemsPerRow = 2,
    similarAspectMargin = 0.1,
    initialRowLimit = 1
  }: {
    viewportWidth: number
    viewportHeight: number
    viewportBound?: boolean
    defaultImageAspect?: number
    leftToRight?: boolean
    itemsPerRow?: number
    similarAspectMargin?: number
    initialRowLimit?: number
  }): LayoutResult {
    const result = this.lastResult.slice()
    const length = result.length

    const row: {
      width: number
      height: number
      items: LayoutImage[]
    } = {
      width: 0,
      height: 0,
      items: []
    }

    const rowAdd = (item: LayoutImage) => {
      row.width += item.width
      row.height = Math.max(row.height, item.height)
      row.items.push(item)
    }

    let y = 0
    let flushed = 0

    const rowFlush = () => {
      if (!row.items.length)
        return

      let scale = 1

      // scale to fit height
      if (viewportBound) {
        scale = Math.min(1, viewportHeight / row.height)

        row.width *= scale
        row.height = viewportHeight // when viewport-bound, we want rows to use the entire viewport height
      }

      row.width = Math.round(row.width)
      row.height = Math.round(row.height)

      let x = (viewportWidth - row.width) / 2

      for (let i = 0; i < row.items.length; i++) {
        const current = row.items[i]
        const last = result[flushed]

        current.width = Math.round(current.width * scale)
        current.height = Math.round(current.height * scale)

        current.x = Math.round(x)
        current.y = Math.round(y + (row.height - current.height) / 2)

        // reverse x if rtl
        if (!leftToRight)
          current.x = viewportWidth - current.x - current.width

        // only change layout identity if layout changed
        if (current.image !== last.image || current.x !== last.x || current.y !== last.y || current.width !== last.width || current.height !== last.height)
          result[flushed] = current

        x += current.width
        flushed++
      }

      // overflow to next row
      y += row.height

      row.width = 0
      row.height = 0
      row.items = []
    }

    for (let i = 0; i < length; i++) {
      const image = images[i]

      // find image dimensions
      let width: number
      let height: number

      if (image?.done === true) {
        width = image.width
        height = image.height
      }
      else {
        width = defaultImageAspect
        height = 1
      }

      // put landscapes in their own row
      if (width >= height) {
        const scale = Math.min(1, viewportWidth / width)

        width *= scale
        height *= scale

        rowFlush()
        rowAdd({ x: 0, y: 0, width, height, image })
        rowFlush()
      }

      else {
        const scale = viewportWidth / itemsPerRow / width

        width *= scale
        height *= scale

        // flush row if full
        if (row.items.length >= itemsPerRow || (flushed === 0 && row.items.length >= initialRowLimit))
          rowFlush()

        // add to row if empty
        if (!row.items.length) {
          rowAdd({ x: 0, y: 0, width, height, image })
        }

        else {
          const aspect = width / height
          const rowItemAspect = row.items[0].width / row.items[0].height

          // flush row if item aspect ratios are too different
          if (Math.abs(aspect - rowItemAspect) > similarAspectMargin)
            rowFlush()

          // add to row
          rowAdd({ x: 0, y: 0, width, height, image })
        }
      }
    }

    // flush remaining
    rowFlush()

    let cause: LayoutResult['cause']

    if (images === this.lastImages)
      cause = 'variable'
    else
      cause = 'images'

    this.lastResult = result
    this.lastImages = images

    return {
      width: viewportWidth,
      height: y,
      layout: result,
      cause
    }
  }
}
