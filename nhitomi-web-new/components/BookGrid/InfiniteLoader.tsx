import React, { memo, useRef, useState } from "react";
import { Center, Icon, Spinner } from "@chakra-ui/react";
import VisibilitySensor from "react-visibility-sensor";
import { useWindowSize } from "../../utils/window";

const InfiniteLoader = ({ hasMore }: { hasMore: () => Promise<boolean> }) => {
  const visible = useRef(false);
  const running = useRef(false);
  const [end, setEnd] = useState(false);
  const [, height] = useWindowSize() || [0, 0];

  const beginLoad = async () => {
    if (running.current || end) return;
    running.current = true;

    try {
      while (visible.current) {
        try {
          if (!(await hasMore())) {
            setEnd(true);
            break;
          }
        } catch (e) {
          console.error(e);

          setEnd(true);
          break;
        }

        await new Promise((resolve) => setTimeout(resolve));
      }
    } finally {
      running.current = false;
    }
  };

  if (end) {
    return null;
  }

  return (
    <VisibilitySensor
      offset={{
        top: -height,
        bottom: -height,
      }}
      onChange={(v) => {
        visible.current = v;
        v && beginLoad();
      }}
    >
      <Center pt={12} pb={12}>
        <Icon as={Spinner} />
      </Center>
    </VisibilitySensor>
  );
};

export default memo(InfiniteLoader);
