import React, { MouseEvent, ReactNode, useState } from "react";
import { css, cx } from "emotion";
import { animated, useSpring } from "react-spring";
import { Color, getColor } from "../theme";

export const FilledButton = ({ children, color = getColor("gray").opacity(0.5), icon, className, onClick }: {
  children?: ReactNode
  color?: Color
  icon?: ReactNode
  className?: string
  onClick?: (e: MouseEvent<HTMLButtonElement>) => void
}) => {
  const [focus, setFocus] = useState(false);
  const [hover, setHover] = useState(false);
  const [click, setClick] = useState(false);

  const style = useSpring({
    backgroundColor: click ? color.tint(0.25).rgb : color.shade(focus || hover ? 0.25 : 0).rgb
  });

  const iconStyle = useSpring({
    opacity: icon ? 1 : 0
  });

  return (
    <animated.button
      style={style}
      className={cx("text-white rounded-sm overflow-hidden cursor-pointer", className)}
      onClick={onClick}
      onFocus={() => setFocus(true)}
      onBlur={() => setFocus(false)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onMouseDown={() => setClick(true)}
      onMouseUp={() => setClick(false)}>

      <div className='px-2 py-1 flex flex-row'>
        {icon && (
          <animated.div style={iconStyle} className={cx("text-center mr-1", css`min-width: 1em;`)} children={icon} />
        )}

        <div children={children} />
      </div>
    </animated.button>
  );
};
