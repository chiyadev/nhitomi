import React, { Dispatch, ReactNode, useMemo, useState } from "react";
import { animated, useSpring } from "react-spring";
import { LeftOutlined, MenuOutlined } from "@ant-design/icons";
import { getColor } from "../theme";
import { useWindowScroll } from "../hooks";
import { useLayout } from "../LayoutManager";
import { cx } from "emotion";
import ScrollLock, { TouchScrollable } from "react-scrolllock";
import { Strip } from "./Strip";
import { useNavigated } from "../state";
import { Footer } from "../Footer";
import { RoundIconButton } from "../Components/RoundIconButton";
import { FormattedMessage } from "react-intl";
import { Tooltip } from "../Components/Tooltip";

export const Overlay = ({ children }: { children?: ReactNode }) => {
  const [open, setOpen] = useState(false);

  useNavigated(() => setTimeout(() => setOpen(false)));

  return useMemo(
    () => (
      <>
        <ScrollLock isActive={open} />

        <Menu open={open} setOpen={setOpen} />
        <Anchor open={open} setOpen={setOpen} />
        <Body open={open} children={children} />
      </>
    ),
    [children, open]
  );
};

const Anchor = ({
  open,
  setOpen,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
}) => {
  const [hover, setHover] = useState(false);
  const [visible, setVisible] = useState(!open);

  const style = useSpring({
    from: {
      opacity: 0,
      marginLeft: -5,
    },
    to: {
      opacity: open ? 0 : 1,
      marginLeft: open ? -5 : 0,
      backgroundColor: getColor("gray", "darkest").tint(hover ? 0.25 : 0).rgb,
    },
    onChange: {
      opacity: (v) => setVisible(v > 0),
    },
  });

  return (
    <animated.div
      style={style}
      className={cx(
        "fixed top-0 left-0 z-10 mt-16 w-10 h-10 flex items-center justify-center rounded-r overflow-hidden shadow-md cursor-pointer",
        { "pointer-events-none": !visible }
      )}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onClick={() => setOpen(true)}
    >
      <MenuOutlined />
    </animated.div>
  );
};

const Body = ({ open, children }: { open: boolean; children?: ReactNode }) => {
  const { width, height } = useLayout();
  const { x: scrollX, y: scrollY } = useWindowScroll();

  const style = useSpring({
    opacity: open ? 0.5 : 1,
    transform: open ? "scale(0.9)" : "scale(1)",
  });

  return (
    <animated.div
      children={children}
      className={cx({ "pointer-events-none": open })}
      style={{
        ...style,
        transformOrigin: `${
          ((scrollX + width / 2) / document.body.clientWidth) * 100
        }% ${((scrollY + height / 2) / document.body.clientHeight) * 100}%`,
      }}
    />
  );
};

const Menu = ({
  open,
  setOpen,
}: {
  open: boolean;
  setOpen: Dispatch<boolean>;
}) => {
  const [visible, setVisible] = useState(open);

  const style = useSpring({
    opacity: open ? 1 : 0,
    transform: open ? "translateX(0)" : "translateX(-10px)",
    onChange: {
      opacity: (v) => setVisible(v > 0),
    },
  });

  return (
    <TouchScrollable>
      <animated.div
        style={style}
        className={cx(
          "fixed top-0 left-0 z-10 w-screen h-screen bg-black bg-blur",
          { "pointer-events-none": !visible }
        )}
        onClick={() => setOpen(false)}
      >
        <Strip
          additionalMenu={
            <Tooltip
              overlay={<FormattedMessage id="components.sidebar.close" />}
              placement="right"
            >
              <RoundIconButton onClick={() => setOpen(false)}>
                <LeftOutlined />
              </RoundIconButton>
            </Tooltip>
          }
        />

        <div className="absolute w-full bottom-0">
          <Footer />
        </div>
      </animated.div>
    </TouchScrollable>
  );
};
