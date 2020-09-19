import React, { ReactNode } from "react";
import { animated, useSpring } from "react-spring";
import { cx } from "emotion";

export const Disableable = ({ children, className, disabled }: { children?: ReactNode, className?: string, disabled?: boolean }) => {
  const style = useSpring({
    opacity: disabled ? 0.5 : 1
  });

  return (
    <animated.div
      style={style}
      className={className}>

      <div className={cx("display-contents", { "pointer-events-none select-none cursor-default": disabled })} children={children} />
    </animated.div>
  );
};
