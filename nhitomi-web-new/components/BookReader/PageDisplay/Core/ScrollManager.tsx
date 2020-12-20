import { memo, RefObject, useEffect } from "react";
import { LayoutResult } from "./layoutEngine";
import { useLastValue, useWindowScroll, useWindowSize } from "../../../../utils/hooks";
import { useReaderScroll } from "../../scroll";

const ScrollManager = ({ containerRef, layout }: { containerRef: RefObject<HTMLElement>; layout: LayoutResult }) => {
  const [, height] = useWindowSize();
  const [, scrollY] = useWindowScroll();

  const [current, setCurrent] = useReaderScroll();
  const last = useLastValue(current);

  const layoutTop = scrollY + (containerRef.current?.getBoundingClientRect().top || 0);

  // detect passive current row
  const mid = scrollY - layoutTop + height / 2;

  useEffect(() => {
    let pages = 0;
    let rows = 0;

    findRow: for (const row of layout.rows) {
      for (const image of row.images) {
        // consider first row in the middle of viewport to be the current row
        if (image.y <= mid && mid < image.y + image.height) {
          if (current.currentPage !== pages || current.currentRow !== rows) {
            setCurrent({ currentPage: pages, currentRow: rows });
          }

          break findRow;
        }

        pages++;
      }

      rows++;
    }

    if (typeof current.inducedPage !== "undefined") {
      if (last.inducedPage !== current.inducedPage) {
        const page = layout.images[Math.max(0, Math.min(layout.images.length - 1, current.inducedPage || 0))];
        const pageMid = page.y + page.height / 2;

        window.scrollTo({ top: layoutTop + pageMid - height / 2 });
      }

      setCurrent((state) => ({ ...state, inducedPage: undefined }));
    }

    if (typeof current.inducedRow !== "undefined") {
      if (last.inducedRow !== current.inducedRow) {
        const row = layout.rows[Math.max(0, Math.min(layout.rows.length - 1, current.inducedRow || 0))];

        // find average middle of row
        let rowMid = 0;

        for (const image of row.images) {
          rowMid += image.y + image.height / 2;
        }

        rowMid /= row.images.length;

        window.scrollTo({ top: layoutTop + rowMid - height / 2 });
      }

      setCurrent((state) => ({ ...state, inducedRow: undefined }));
    }
  }, [current, setCurrent, last, height, layout, layoutTop, mid]);

  return null;
};

export default memo(ScrollManager);
