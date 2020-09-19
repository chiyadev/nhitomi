import React, { ReactNode, useRef, useState } from "react";
import { cx } from "emotion";
import useResizeObserver from "@react-hook/resize-observer";
import { getBreakpoint, LargeBreakpoints } from "../LayoutManager";

export const Container = ({ children, className }: { children?: ReactNode, className?: string }) => {
  const measureRef = useRef<HTMLDivElement>(null);
  const [parentWidth, setParentWidth] = useState(0);

  useResizeObserver(measureRef, ({ contentRect: { width } }) => setParentWidth(width));

  const width = getBreakpoint(LargeBreakpoints, parentWidth);

  return (
    <div ref={measureRef} className='w-full'>
      <div style={{ maxWidth: width }} className={cx("relative mx-auto w-full", className)} children={children} />
    </div>
  );
};
