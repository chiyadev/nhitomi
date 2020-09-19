import React, { createContext, ReactNode, useContext, useMemo } from "react";
import { useWindowSize } from "./hooks";

export type ScreenType = "sm" | "lg"

/** On large screens there is a sidebar. */
export const SidebarStripWidth = 64;

/** Width after which the device will be considered large. */
export const ScreenBreakpoint = 768;

export const SmallBreakpoints = [320, 480, 640];
export const LargeBreakpoints = [640, 768, 1024, 1280].map(n => n - SidebarStripWidth);

/** Layout information context. */
const LayoutContext = createContext<{
  width: number
  height: number
  screen: ScreenType
  breakpoint?: number
}>(undefined as any);

export function useLayout() {
  return useContext(LayoutContext);
}

export const LayoutManager = ({ children }: { children?: ReactNode }) => {
  const { width: windowWidth, height } = useWindowSize();

  return (
    <LayoutContext.Provider
      children={children}
      value={useMemo(() => {
        let screen: ScreenType = "sm";
        let width = windowWidth;
        let breakpoint = getBreakpoint(SmallBreakpoints, width);

        if (width >= ScreenBreakpoint) {
          screen = "lg";
          width -= SidebarStripWidth;
          breakpoint = getBreakpoint(LargeBreakpoints, width);
        }

        return { screen, width, height, breakpoint };
      }, [windowWidth, height])} />
  );
};

export function getBreakpoint(breakpoints: number[], value: number) {
  let breakpoint: number | undefined;

  for (const br of breakpoints) {
    if (value >= br)
      breakpoint = br;

    else break;
  }

  return breakpoint;
}
