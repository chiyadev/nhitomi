import { RefObject, useLayoutEffect, useRef } from "react";
import { useWindowScroll } from "../hooks";
import { useLayout } from "../LayoutManager";
import { LayoutResult } from "./layoutEngine";
import { usePageState } from "../state";

export type CurrentPage = {
  rowInduced?: number;
  rowPassive: number;

  pageInduced?: number;
  pagePassive: number;
};

export const ScrollManager = ({
  containerRef,
  layout,
}: {
  containerRef: RefObject<HTMLDivElement>;
  layout: LayoutResult;
}) => {
  const { height } = useLayout();
  const [current, setCurrent] = usePageState<CurrentPage>("page", {
    rowPassive: 0,
    pagePassive: 0,
  });

  const lastRef = useRef(current);
  const last = lastRef.current;

  // scroll position relative to layout container
  const layoutOffset = containerRef.current?.offsetTop || 0;

  // detect passive current row
  const mid = useWindowScroll().y - layoutOffset + height / 2;

  useLayoutEffect(() => {
    let rows = 0;
    let pages = 0;

    findRow: for (const row of layout.rows) {
      for (const image of row.images) {
        // consider first row in the middle of viewport to be the current row
        if (image.y <= mid && mid < image.y + image.height) {
          if (current.rowPassive !== rows || current.pagePassive !== pages)
            setCurrent({ rowPassive: rows, pagePassive: pages });

          break findRow;
        }

        pages++;
      }

      rows++;
    }

    if (typeof current.pageInduced === "number" && last.pageInduced !== current.pageInduced) {
      const page = layout.images[Math.max(0, Math.min(layout.images.length - 1, current.pageInduced || 0))];
      const pageMid = page.y + page.height / 2;

      window.scrollTo({ top: layoutOffset + pageMid - height / 2 });
    }

    if (typeof current.rowInduced === "number" && last.rowInduced !== current.rowInduced) {
      const row = layout.rows[Math.max(0, Math.min(layout.rows.length - 1, current.rowInduced || 0))];

      let rowMid = 0;

      for (const image of row.images) rowMid += image.y + image.height / 2;

      rowMid /= row.images.length;

      window.scrollTo({ top: layoutOffset + rowMid - height / 2 });
    }

    lastRef.current = current;
  }, [current, height, last, layout, layoutOffset, mid, setCurrent]);

  return null;
};
