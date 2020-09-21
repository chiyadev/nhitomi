import React from "react";
import { Line } from "rc-progress";
import { Color, getColor } from "../theme";

export const Progress = ({
  value,
  className,
  color = getColor("blue"),
  trailColor = getColor("gray").opacity(0.5),
}: {
  value: number;
  className?: string;
  color?: Color;
  trailColor?: Color;
}) => {
  return <Line percent={value * 100} className={className} strokeColor={color.rgb} trailColor={trailColor.rgb} />;
};
