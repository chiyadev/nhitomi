import React, { Dispatch, ReactNode, useState } from "react";
import { cx } from "emotion";
import { CheckOutlined } from "@ant-design/icons";
import { animated, useSpring } from "react-spring";
import { Color, getColor } from "../theme";

export type CheckBoxType = "check" | "radio";

export const CheckBox = ({
  value,
  setValue,
  type = "check",
  color = getColor("blue"),
  children,
  className,
}: {
  value?: boolean;
  setValue?: Dispatch<boolean>;
  type?: CheckBoxType;
  color?: Color;
  children?: ReactNode;
  className?: string;
}) => {
  const [hover, setHover] = useState(false);

  return (
    <div
      className={cx("flex flex-row items-center cursor-pointer", className)}
      onClick={() => setValue?.(!value)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
    >
      <div className="mx-2 my-1">
        {type === "check" ? (
          <Check value={value} hover={hover} color={color} />
        ) : type === "radio" ? (
          <Radio value={value} hover={hover} color={color} />
        ) : null}
      </div>

      <div className="flex-1 mr-2 truncate">{children}</div>
    </div>
  );
};

const Check = ({
  value,
  hover,
  color,
}: {
  value?: boolean;
  hover?: boolean;
  color: Color;
}) => {
  const boxStyle = useSpring({
    backgroundColor: color
      .opacity(value ? 1 : 0)
      .tint(value && hover ? 0.25 : 0).rgb,
    borderColor: (value ? color : getColor("white").opacity(0.25)).tint(
      hover ? 0.25 : 0
    ).rgb,
  });

  const checkStyle = useSpring({
    opacity: value ? 1 : 0,
    marginBottom: value ? 0 : -2,
    transform: value ? "scale(1)" : "scale(0.8)",
  });

  return (
    <animated.div
      style={boxStyle}
      className="w-4 h-4 flex items-center text-center rounded border box-content"
    >
      <animated.span style={checkStyle} className="flex-1 text-xs">
        <CheckOutlined />
      </animated.span>
    </animated.div>
  );
};

const Radio = ({
  value,
  hover,
  color,
}: {
  value?: boolean;
  hover?: boolean;
  color: Color;
}) => {
  const circleStyle = useSpring({
    borderColor: (value ? color : getColor("white").opacity(0.25)).tint(
      hover ? 0.25 : 0
    ).rgb,
  });

  const dotStyle = useSpring({
    backgroundColor: color.tint(hover ? 0.25 : 0).rgb,
    opacity: value ? 1 : 0,
    transform: value ? "scale(0.6)" : "scale(0)",
  });

  return (
    <animated.div
      style={circleStyle}
      className="w-4 h-4 flex items-center justify-center rounded-full border box-content"
    >
      <animated.span style={dotStyle} className="w-4 h-4 rounded-full" />
    </animated.div>
  );
};
