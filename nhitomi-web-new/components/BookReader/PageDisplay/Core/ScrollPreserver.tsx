import { memo, RefObject, useRef } from "react";
import { isSafari, safariResizeDelay } from "../../../../utils/fuckSafari";
import { LayoutResult } from "./layoutEngine";
import { useLastValue, useWindowScroll, useWindowSize } from "../../../../utils/hooks";
import { useBreakpointValue } from "@chakra-ui/media-query";

const ScrollPreserver = ({ containerRef, layout }: { containerRef: RefObject<HTMLElement>; layout: LayoutResult }) => {
  const container = containerRef.current;

  // rerender on scroll
  useWindowScroll();

  const lastLayout = useLastValue(layout);

  const [, height] = useWindowSize();
  const visible = useRef<HTMLElement>();
  const scrolling = useRef<number>();

  const scrollMode = useBreakpointValue({ base: "nearest", md: "center" });

  if (!scrolling.current) {
    if (layout.cause !== "images" && (layout.width !== lastLayout.width || layout.height !== lastLayout.height)) {
      const scroll = () => {
        visible.current?.scrollIntoView({ block: scrollMode as any });
        scrolling.current = undefined;
      };

      if (isSafari) scrolling.current = window.setTimeout(scroll, safariResizeDelay);
      else scrolling.current = requestAnimationFrame(scroll);
    } else if (container) {
      let visibleElement: HTMLElement | undefined;

      for (let i = 0; i < container.children.length; i++) {
        const child = container.children[i];

        if (child instanceof HTMLElement) {
          // child is considered visible if they're in the middle of the window
          const { top, bottom } = child.getBoundingClientRect();

          if (top < height / 2 && height / 2 < bottom) {
            visibleElement = child;
            break;
          }
        }
      }

      visible.current = visibleElement;
    }
  }

  return null;
};

export default memo(ScrollPreserver);
