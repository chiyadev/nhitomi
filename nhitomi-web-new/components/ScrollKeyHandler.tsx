import { memo, useEffect, useRef } from "react";
import { useHotkeyState, useWindowSize } from "../utils/hooks";
import { useSpring } from "framer-motion";

const ScrollKeyHandler = () => {
  const scrollUp = useHotkeyState("up, w");
  const scrollDown = useHotkeyState("down, s");

  const [, height] = typeof window === "undefined" ? [0, 0] : useWindowSize();

  const motion = useSpring(0, { bounce: 0, damping: 5, mass: 0.1 });
  const direction = useRef(0);

  useEffect(() => {
    motion.set(scrollUp || scrollDown ? height / 500 : 0);

    delta.current = 0;
    direction.current = scrollUp && scrollDown ? 0 : scrollUp ? -1 : scrollDown ? 1 : direction.current;
    timestamp.current = performance.now();
  }, [motion, scrollUp, scrollDown, height]);

  const timeout = useRef<number>();
  const timestamp = useRef(0);
  const delta = useRef(0);

  useEffect(() => {
    return motion.onChange((value) => {
      timeout.current && cancelAnimationFrame(timeout.current);

      if (value) {
        const frame = (time: number) => {
          const elapsed = Math.max(0, time - timestamp.current);

          delta.current += elapsed * value * direction.current;

          const scroll = Math.floor(delta.current);

          delta.current -= scroll;

          window.scrollBy({ top: scroll });

          timestamp.current = time;
          timeout.current = requestAnimationFrame(frame);
        };

        timeout.current = requestAnimationFrame(frame);
      } else {
        timeout.current = undefined;
      }
    });
  }, [motion]);

  return null;
};

export default memo(ScrollKeyHandler);
