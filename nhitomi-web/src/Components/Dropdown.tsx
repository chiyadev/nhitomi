import React, { ComponentProps, ReactNode, useState } from "react";
import { Tooltip } from "./Tooltip";
import { animated, useSpring } from "react-spring";
import { convertHex } from "../theme";
import { css, cx } from "emotion";
import { RightOutlined } from "@ant-design/icons";

export const Dropdown = ({
  interactive = true,
  placement = "bottom-start",
  touch = true,
  padding = true,
  scaleTransition = true,
  flip = false,
  overlayClassName,
  ...props
}: ComponentProps<typeof Tooltip>) => {
  return (
    <Tooltip
      interactive={interactive}
      placement={placement}
      touch={touch}
      padding={false}
      scaleTransition={scaleTransition}
      flip={flip}
      overlayClassName={cx({ "py-2": padding }, overlayClassName)}
      {...props}
    />
  );
};

export const DropdownItem = ({
  children,
  className,
  padding = true,
  icon,
  onClick,
}: {
  children?: ReactNode;
  className?: string;
  padding?: boolean;
  icon?: ReactNode;
  onClick?: () => void;
}) => {
  const [hover, setHover] = useState(false);
  const [click, setClick] = useState(false);

  const style = useSpring({
    backgroundColor: convertHex("#fff", click ? 0.25 : hover ? 0.125 : 0),
  });

  const iconStyle = useSpring({
    opacity: icon ? 1 : 0,
  });

  return (
    <animated.div
      style={style}
      className={cx("cursor-pointer flex flex-row", { "px-2 py-1": padding }, className)}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onMouseDown={() => setClick(true)}
      onMouseUp={() => setClick(false)}
      onClick={onClick}
    >
      {icon && (
        <animated.div style={iconStyle} className="w-4 text-center mr-1">
          {icon}
        </animated.div>
      )}

      <div className="flex-1 truncate">{children}</div>
    </animated.div>
  );
};

export const DropdownGroup = ({
  name,
  children,
  className,
}: {
  name?: ReactNode;
  children?: ReactNode;
  className?: string;
}) => (
  <div className={cx("pl-2", className)}>
    <div className="text-gray-darker cursor-default py-1 truncate">{name}</div>
    <div className="rounded-l-sm overflow-hidden">{children}</div>
  </div>
);

export const DropdownSubMenu = ({
  name,
  children,
  onShow,
  onHide,
  ...props
}: { name?: ReactNode } & ComponentProps<typeof DropdownItem> &
  Pick<ComponentProps<typeof Dropdown>, "onShow" | "onHide">) => (
  <Dropdown
    appendTo="parent"
    overlay={children}
    placement="right-start"
    offset={[0, 3]}
    blurred={false} // 2020/08 there is a bug with Chrome that causes nested absolute backdrop-filters to not work
    moveTransition
    scaleTransition={false}
    flip
    onShow={onShow}
    onHide={onHide}
  >
    <DropdownItem {...props}>
      {name}

      <div className="ml-1 float-right h-full flex items-center">
        <RightOutlined />
      </div>
    </DropdownItem>
  </Dropdown>
);

export const DropdownDivider = ({ className }: { className?: string }) => (
  <div
    className={cx(
      "mx-2 my-2 bg-gray",
      className,
      css`
        height: 1px;
        opacity: 15%;
      `
    )}
  />
);
