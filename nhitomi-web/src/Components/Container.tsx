import React, { ReactNode, useRef } from "react";
import { cx } from "emotion";
import { useSize } from "../hooks";
import { getBreakpoint, LargeBreakpoints } from "../LayoutManager";

export const Container = ({ children, className }: { children?: ReactNode; className?: string }) => {
  const measureRef = useRef<HTMLDivElement>(null);

  const parentWidth = useSize(measureRef)?.width || 0;
  const width = getBreakpoint(LargeBreakpoints, parentWidth);

  return (
    <div ref={measureRef} className="w-full">
      <div style={{ maxWidth: width }} className={cx("relative mx-auto w-full", className)}>
        {children}
      </div>
    </div>
  );
};
