import React, { ReactNode, useState } from "react";
import VisibilitySensor from "react-visibility-sensor";
import { useAsync } from "../hooks";
import { cx } from "emotion";
import { animated, useSpring } from "react-spring";
import { Loading3QuartersOutlined } from "@ant-design/icons";

export const LoadContainer = ({
  onLoad,
  children,
  className,
}: {
  onLoad: () => Promise<void> | void;
  children?: ReactNode;
  className?: string;
}) => {
  const [load, setLoad] = useState(false);

  const { loading } = useAsync(async () => {
    if (load) await onLoad();
  }, [load]);

  const loadingStyle = useSpring({
    opacity: loading ? 1 : 0,
  });

  return (
    <VisibilitySensor
      delayedCall
      partialVisibility
      onChange={(v) => v && setLoad(true)}
      offset={{ top: -400, left: -400, bottom: -400, right: -400 }}
    >
      <div className={cx("relative", className)}>
        {children}

        <animated.div
          style={loadingStyle}
          className="absolute transform-center"
        >
          <Loading3QuartersOutlined className="animate-spin" />
        </animated.div>
      </div>
    </VisibilitySensor>
  );
};
