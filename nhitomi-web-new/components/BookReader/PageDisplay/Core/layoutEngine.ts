export type ImageInfo = {
  width: number;
  height: number;
};

export type LayoutImage = {
  image?: ImageInfo;
  x: number;
  y: number;
  width: number;
  height: number;
};

export type LayoutRow = {
  images: LayoutImage[];
};

export type LayoutResult = {
  images: LayoutImage[];
  rows: LayoutRow[];
  width: number;
  height: number;

  /** What caused the layout to be recomputed */
  cause: "variable" | "images";
};

export class LayoutEngine {
  private cache: LayoutImage[] = [];
  private lastImages?: (ImageInfo | undefined)[];

  constructor(size: number) {
    for (let i = 0; i < size; i++) {
      this.cache.push({ x: 0, y: 0, width: 0, height: 0 });
    }
  }

  recompute(
    images: (ImageInfo | undefined)[],
    {
      viewportWidth,
      viewportHeight,
      viewportBound = true,
      defaultImageAspect = 13 / 19,
      leftToRight = false,
      itemsPerRow = 2,
      similarAspectMargin = 0.1,
      initialRowLimit,
    }: {
      viewportWidth: number;
      viewportHeight: number;
      viewportBound?: boolean;
      defaultImageAspect?: number;
      leftToRight?: boolean;
      itemsPerRow?: number;
      similarAspectMargin?: number;
      initialRowLimit?: number;
    }
  ): LayoutResult {
    const result = this.cache.slice();
    const length = result.length;

    const rows: LayoutRow[] = [];

    const row: {
      width: number;
      height: number;
      images: LayoutImage[];
    } = {
      width: 0,
      height: 0,
      images: [],
    };

    const rowAdd = (image: LayoutImage) => {
      row.width += image.width;
      row.height = Math.max(row.height, image.height);
      row.images.push(image);
    };

    let y = 0;
    let flushed = 0;

    const rowFlush = () => {
      if (!row.images.length) return;

      let scale = 1;

      // scale to fit height
      if (viewportBound) {
        scale = Math.min(1, viewportHeight / row.height);

        row.width *= scale;
        row.height = viewportHeight; // when viewport-bound, we want rows to use the entire viewport height
      }

      row.width = Math.round(row.width);
      row.height = Math.round(row.height);

      let x = (viewportWidth - row.width) / 2;

      for (let i = 0; i < row.images.length; i++) {
        const current = row.images[i];
        const last = result[flushed];

        current.width = Math.round(current.width * scale);
        current.height = Math.round(current.height * scale);

        current.x = Math.round(x);
        current.y = Math.round(y + (row.height - current.height) / 2);

        // reverse x if rtl
        if (!leftToRight) current.x = viewportWidth - (current.x + current.width);

        // only change layout identity if layout changed
        if (
          current.image !== last.image ||
          current.x !== last.x ||
          current.y !== last.y ||
          current.width !== last.width ||
          current.height !== last.height
        )
          result[flushed] = current;

        x += current.width;
        flushed++;
      }

      // overflow to next row
      y += row.height;

      rows.push({ images: row.images.slice() });

      row.width = 0;
      row.height = 0;
      row.images = [];
    };

    for (let i = 0; i < length; i++) {
      const image = images[i];

      // find image dimensions
      let width: number;
      let height: number;

      if (image) {
        width = image.width;
        height = image.height;
      } else {
        width = defaultImageAspect;
        height = 1;
      }

      // put landscapes in their own row
      if (width >= height) {
        const scale = Math.min(1, viewportWidth / width);

        width *= scale;
        height *= scale;

        rowFlush();
        rowAdd({ x: 0, y: 0, width, height, image });
        rowFlush();
      } else {
        const scale = viewportWidth / itemsPerRow / width;

        width *= scale;
        height *= scale;

        // flush row if full
        if (
          row.images.length >= itemsPerRow ||
          (flushed === 0 && initialRowLimit && row.images.length >= initialRowLimit)
        )
          rowFlush();

        // add to row if empty
        if (!row.images.length) {
          rowAdd({ x: 0, y: 0, width, height, image });
        } else {
          const aspect = width / height;
          const rowItemAspect = row.images[0].width / row.images[0].height;

          // flush row if item aspect ratios are too different
          if (Math.abs(aspect - rowItemAspect) > similarAspectMargin) rowFlush();

          // add to row
          rowAdd({ x: 0, y: 0, width, height, image });
        }
      }
    }

    // flush remaining
    rowFlush();

    let cause: LayoutResult["cause"];

    if (images === this.lastImages) {
      cause = "variable";
    } else {
      cause = "images";
    }

    this.cache = result;
    this.lastImages = images;

    return {
      width: viewportWidth,
      height: y,
      images: result,
      rows,
      cause,
    };
  }
}
