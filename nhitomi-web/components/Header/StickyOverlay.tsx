import React, { memo, ReactNode, useEffect, useRef, useState } from "react";
import { useInView } from "react-intersection-observer";
import { useLastValue, useWindowScroll } from "../../utils/hooks";
import { chakra } from "@chakra-ui/react";

const StickyOverlay = ({ shadow, children }: { shadow?: boolean; children?: ReactNode }) => {
  const [ref, , intersection] = useInView({
    threshold: [1],
  });

  const overlay = intersection && intersection.intersectionRatio < 1;

  const [hide, setHide] = useState(false);
  const [, scrollY] = typeof window === "undefined" ? [0, 0] : useWindowScroll();
  const lastScrollY = useLastValue(scrollY);
  const lastScrollDown = useRef(scrollY);

  useEffect(() => {
    if (scrollY > lastScrollY) {
      lastScrollDown.current = scrollY;
    }

    setHide(scrollY >= lastScrollDown.current - 100);
  }, [scrollY]);

  return (
    <chakra.div ref={ref} as="nav" position="sticky" zIndex="sticky" top="-1px" overflow="hidden">
      <chakra.div
        bg="gray.900"
        boxShadow={(overlay || shadow) && !hide ? "lg" : undefined}
        transform={overlay && hide ? "translateY(-100%)" : undefined}
        transition="all .3s cubic-bezier(0.16, 1, 0.3, 1)"
      >
        {children}
      </chakra.div>
    </chakra.div>
  );
};

export default memo(StickyOverlay);
