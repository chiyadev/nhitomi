import React, { ReactNode, useLayoutEffect } from "react";
import { animated, useSpring } from "react-spring";
import { usePageState } from "../state";

export const PageContainer = ({ children, className }: { children?: ReactNode; className?: string }) => {
  const [shown, setShown] = usePageState<boolean>("pageShown");

  // prevents fade-in on back navigate
  useLayoutEffect(() => setShown(true), [setShown]);

  const style = useSpring({
    from: { opacity: shown ? 1 : 0 },
    to: { opacity: 1 },
  });

  return (
    <animated.div style={style} className={className}>
      {children}
    </animated.div>
  );
};
